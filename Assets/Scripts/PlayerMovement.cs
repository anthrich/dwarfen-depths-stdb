using System;
using JetBrains.Annotations;
using SpacetimeDB;
using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Input = SpacetimeDB.Types.Input;

public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;
    public EntityInterpolation entityInterpolation;
    public Transform serverStateObject;
    public bool applyReconciliation = true;
    public bool applyPrediction = true;

    private const int CacheSize = 1024;
    private float _serverUpdateInterval;
    private float MovementSpeed => _serverEntityState.Speed;
    private float SpeedPerInterval => MovementSpeed * _serverUpdateInterval;
    
    private Vector2 _movement = Vector2.zero;
    private float _accumulatedDeltaTime;
    private ulong _currentSequenceId;
    private Entity _serverEntityState = new();
    private ulong _lastCorrectedSequenceId;
    private float _yPosition;
    
    private readonly SimulationState[] _simulationStateCache = new SimulationState[CacheSize];
    private readonly InputState[] _inputStateCache = new InputState[CacheSize];

    public struct InputState
    {
        public static bool IsDefault(InputState @is) =>
            @is.Direction == Vector2.zero && @is.SequenceId == 0;
        
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
        if(entityInterpolation == default) entityInterpolation = GetComponent<EntityInterpolation>();
        if (serverStateObject == default) serverStateObject = transform.GetChild(0);
        entityInterpolation.SetCanonicalPosition(transform.position);
        _yPosition = transform.position.y;
        _serverUpdateInterval = GameManager.Config.UpdateEntityInterval;
    }

    public void OnEntitySpawned(Entity newServerEntityState)
    {
        _currentSequenceId = newServerEntityState.SequenceId;
        
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        if (newServerEntityState.SequenceId != _serverEntityState.SequenceId + 1)
        {
            Debug.Log($"Sequence issue: (Old){_serverEntityState.SequenceId} : (New){newServerEntityState.SequenceId}");
        }
        
        _serverEntityState = newServerEntityState;
        serverStateObject.position = newServerEntityState.Position.ToGamePosition(_yPosition);
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
        var canonicalPosition = entityInterpolation.GetCanonicalPosition();
        
        while (_accumulatedDeltaTime >= _serverUpdateInterval)
        {
            _accumulatedDeltaTime -= _serverUpdateInterval;
            var cacheIndex = Convert.ToInt32(_currentSequenceId % CacheSize);
            var input = GetInput();
            _inputStateCache[cacheIndex] = input;
            _simulationStateCache[cacheIndex] = new SimulationState
            {
                Position = new Vector2(canonicalPosition.x, canonicalPosition.z),
                SequenceId = _currentSequenceId
            };
            if (applyPrediction)
            {
                canonicalPosition = ApplyDirection(input.Direction, canonicalPosition);
            }
            SendInput();
            _currentSequenceId++;
        }
        
        if(applyReconciliation) canonicalPosition = Reconcile(canonicalPosition);
        
        entityInterpolation.SetCanonicalPosition(canonicalPosition);
    }
    
    private Vector3 ApplyDirection(Vector2 direction, Vector3 targetPosition)
    {
        var movement = direction * SpeedPerInterval;
        return targetPosition + new Vector3(movement.x, 0, movement.y);
    }

    private void SendInput()
    {
        var direction = new DbVector2(_movement.x, _movement.y);
        GameManager.Conn.Reducers.UpdatePlayerInput(new Input(_currentSequenceId, direction));
    }
    
    private InputState GetInput()
    {
        return new InputState
        {
            Direction = _movement,
            SequenceId = _currentSequenceId,
        };
    }

    private Vector3 Reconcile(Vector3 canonicalPosition)
    {
        if(_serverEntityState.SequenceId <= _lastCorrectedSequenceId) return canonicalPosition;
        var cacheIndex = Convert.ToInt32(_serverEntityState.SequenceId % CacheSize);
        var cachedSimulationState = _simulationStateCache[cacheIndex];
        var serverPosition = _serverEntityState.Position.ToUnityVector2();
        var posDif = Vector2.Distance(cachedSimulationState.Position, serverPosition);
        
        if (posDif > 0.001f)
        {
            canonicalPosition = new Vector3(serverPosition.x, _yPosition, serverPosition.y);
            var rewindTick = _serverEntityState.SequenceId + 1;
            
            while (rewindTick < _currentSequenceId)
            {
                var rewindCacheIndex = Convert.ToInt32(rewindTick % CacheSize);
                var rewoundInput = _inputStateCache[rewindCacheIndex];
                var rewoundSimulation = _simulationStateCache[rewindCacheIndex];
                
                if (InputState.IsDefault(rewoundInput) || SimulationState.IsDefault(rewoundSimulation))
                {
                    ++rewindTick;
                    continue;
                }

                if (applyPrediction)
                {
                    canonicalPosition = ApplyDirection(rewoundInput.Direction, canonicalPosition);
                }
                
                _simulationStateCache[rewindCacheIndex] = new SimulationState
                {
                    Position = canonicalPosition,
                    SequenceId = rewindTick
                };
                
                ++rewindTick;
            }
        }
        
        _lastCorrectedSequenceId = _serverEntityState.SequenceId;
        
        return canonicalPosition;
    }
}
