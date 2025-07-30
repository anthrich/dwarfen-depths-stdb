using System;
using JetBrains.Annotations;
using SpacetimeDB;
using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;
    public EntityController entityController;
    public Transform serverStateObject;
    public bool applyReconcilliation = true;

    private const int CacheSize = 1024;
    private const float ServerUpdateInterval = 0.05f;
    
    private Vector2 _movement = Vector2.zero;
    private float _accumulatedDeltaTime;
    private ulong _currentSequenceId;
    private Entity _serverEntityState = new();
    private ulong _lastCorrectedSequenceId;
    private DateTimeOffset _lastUpdateTime;
    
    private readonly SimulationState[] _simulationStateCache = new SimulationState[CacheSize];
    private readonly InputState[] _inputStateCache = new InputState[CacheSize];

    public struct InputState
    {
        public static bool IsDefault(InputState inputState) =>
            inputState.Direction == Vector2.zero && inputState.SequenceId == 0;
        
        public Vector2 Direction;
        public ulong SequenceId;
    }

    public struct SimulationState
    {
        public static bool IsDefault(SimulationState simState) =>
            simState.Position == Vector2.zero && simState.SequenceId == 0;
        
        public Vector2 Position;
        public ulong SequenceId;
    }
    
    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
        if(entityController == default) entityController = GetComponent<EntityController>();
        if (serverStateObject == default) serverStateObject = transform.GetChild(0);
        _lastUpdateTime = DateTimeOffset.Now;
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        if (newServerEntityState.SequenceId < _serverEntityState.SequenceId) return;
        _serverEntityState = newServerEntityState;
        serverStateObject.position = newServerEntityState.Position.ToGamePosition(transform.position.y);
    }

    [UsedImplicitly]
    private void OnMove(InputValue value)
    {
        var movementVector = value.Get<Vector2>();
        var newMovement = ApplyCameraHeading(movementVector);
        if(newMovement.ApproximatesTo(_movement)) return;
        _movement = newMovement;
    }

    private Vector2 ApplyCameraHeading(Vector2 movementVector)
    {
        var convertedVector3 =  new Vector3(movementVector.x, 0, movementVector.y);
        var transformedMovement = cameraTransform.TransformDirection(convertedVector3);
        transformedMovement.y = 0;
        transformedMovement = transformedMovement.normalized;
        var newMovement = new Vector2(transformedMovement.x, transformedMovement.z);
        return newMovement;
    }

    private void Update()
    {
        _accumulatedDeltaTime += Time.deltaTime;
        while (_accumulatedDeltaTime >= ServerUpdateInterval)
        {
            var updateTime = DateTimeOffset.Now - _lastUpdateTime;
            _lastUpdateTime = DateTimeOffset.Now;
            _accumulatedDeltaTime -= ServerUpdateInterval;
            var cacheIndex = Convert.ToInt32(_currentSequenceId % CacheSize);
            var input = GetInput();
            _inputStateCache[cacheIndex] = input;
            _simulationStateCache[cacheIndex] = CurrentSimulationState();
            entityController.ApplyDirection(input.Direction, updateTime.Milliseconds / 1000f);
            SendInput();
            _currentSequenceId++;
        }
        
        if(applyReconcilliation) Reconcile();
    }

    private void SendInput()
    {
        var direction = new DbVector2(_movement.x, _movement.y);
        GameManager.Conn.Reducers.UpdatePlayerInput(direction, _currentSequenceId);
    }
    
    private InputState GetInput()
    {
        return new InputState
        {
            Direction = _movement,
            SequenceId = _currentSequenceId
        };
    }

    private SimulationState CurrentSimulationState()
    {
        return new SimulationState
        {
            Position = new Vector2(transform.position.x, transform.position.z),
            SequenceId = _currentSequenceId
        };
    }

    private void Reconcile()
    {
        if(_serverEntityState.SequenceId <= _lastCorrectedSequenceId) return;
        var cacheIndex = Convert.ToInt32(_serverEntityState.SequenceId % CacheSize);
        var cachedSimulationState = _simulationStateCache[cacheIndex];
        var serverPosition = _serverEntityState.Position.ToUnityVector2();
        var posDif = Vector2.Distance(cachedSimulationState.Position, serverPosition);
        
        var serverStateMessage =
            $"<color=#5bc18e>{_serverEntityState.SequenceId}:{serverPosition}</color>";
        var cachedStateMessage =
            $"<color=#5bc1c1>{cachedSimulationState.SequenceId}:{cachedSimulationState.Position}</color>";
        
        if (posDif > 0.001f)
        {
            Debug.Log($"Reconciled: {serverStateMessage} : {cachedStateMessage}");
            transform.position = new Vector3(serverPosition.x, transform.position.y, serverPosition.y);
            var rewindTick = _serverEntityState.SequenceId + 1;
            while (rewindTick < _currentSequenceId)
            {
                var rewindCacheIndex = Convert.ToInt32(rewindTick % CacheSize);
                var rewindCachedInputState = _inputStateCache[rewindCacheIndex];
                var rewindCachedSimulationState = _simulationStateCache[rewindCacheIndex];
                
                if (InputState.IsDefault(rewindCachedInputState) ||
                    SimulationState.IsDefault(rewindCachedSimulationState))
                {
                    ++rewindTick;
                    continue;
                }
                Debug.Log($"Rewind: {_serverEntityState.SequenceId} : {rewindTick}");
                entityController.ApplyDirection(rewindCachedInputState.Direction, ServerUpdateInterval);
                _simulationStateCache[rewindCacheIndex] = CurrentSimulationState();
                _simulationStateCache[rewindCacheIndex].SequenceId = rewindTick;
                ++rewindTick;
            }
        }

        _lastCorrectedSequenceId = _serverEntityState.SequenceId;
    }
}
