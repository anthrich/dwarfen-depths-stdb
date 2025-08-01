using System;
using JetBrains.Annotations;
using SpacetimeDB.Types;
using UnityEngine;
using UnityEngine.InputSystem;
using Input = SpacetimeDB.Types.Input;

public class PlayerMovement : MonoBehaviour, IPublisher<UpdateRateCache>
{
    public Transform cameraTransform;
    public EntityInterpolation entityInterpolation;
    public Transform serverStateObject;
    public bool applyReconciliation = true;
    public bool applyPrediction = true;

    private const string ServerUpdateRateKey = "server-update-rate";
    private const string ClientUpdateRateKey = "client-update-rate";
    private const int CacheSize = 1024;
    
    private float _serverUpdateInterval;
    private float MovementSpeed => _serverEntityState.Speed;
    private float SpeedPerInterval => MovementSpeed * _serverUpdateInterval;
    private Vector2 _movement = Vector2.zero;
    private float _accumulatedDeltaTime;
    private float _deltaTimeMultiplier = 1f;
    private ulong _currentSequenceId;
    private Entity _serverEntityState = new();
    private ulong _lastCorrectedSequenceId;
    private float _yPosition;
    private DateTimeOffset _lastServerUpdateAt;
    private DateTimeOffset _lastClientUpdateAt;
    
    private readonly SimulationState[] _simulationStateCache = new SimulationState[CacheSize];
    private readonly InputState[] _inputStateCache = new InputState[CacheSize];
    private readonly UpdateRateCache _updateRateCache = new(30, new []{ ServerUpdateRateKey, ClientUpdateRateKey });

    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
        if(entityInterpolation == default) entityInterpolation = GetComponent<EntityInterpolation>();
        if (serverStateObject == default) serverStateObject = transform.GetChild(0);
        entityInterpolation.SetCanonicalPosition(transform.position);
        _yPosition = transform.position.y;
        _serverUpdateInterval = GameManager.Config.UpdateEntityInterval;
        _lastServerUpdateAt = DateTimeOffset.Now;
        _lastClientUpdateAt = DateTimeOffset.Now;
    }
    
    public void Subscribe(ISubscriber<UpdateRateCache> subscriber)
    {
        _updateRateCache.Subscribe(subscriber);
    }

    public void Unsubscribe(int instanceId)
    {
        _updateRateCache.Unsubscribe(instanceId);
    }

    public void OnEntitySpawned(Entity newServerEntityState)
    {
        _currentSequenceId = newServerEntityState.SequenceId;
        Debug.Log($"Entity spawned: {newServerEntityState}");
    }
    
    [UsedImplicitly]
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        if (_serverEntityState.SequenceId != 0 && newServerEntityState.SequenceId != _serverEntityState.SequenceId + 1)
        {
            Debug.Log($"Sequence issue: (Old){_serverEntityState.SequenceId} : (New){newServerEntityState.SequenceId}");
        }
        
        var diff = DateTimeOffset.Now - _lastServerUpdateAt;
        _lastServerUpdateAt = DateTimeOffset.Now;
        _updateRateCache.AddToStream(
            ServerUpdateRateKey,
            new UpdateRateCache.Entry { Rate = diff.TotalMilliseconds, SequenceId = newServerEntityState.SequenceId }
        );
        _serverEntityState = newServerEntityState;
        serverStateObject.position = newServerEntityState.Position.ToGamePosition(_yPosition);
        
        var sequenceDiff = (long)_currentSequenceId - (long)_serverEntityState.SequenceId;
        var modifier = Mathf.Clamp(0.01f + Math.Abs(sequenceDiff) * 0.002f, 0.01f, 0.03f);
        switch (sequenceDiff)
        {
            case > 5:
                Time.timeScale -= 0.01f + modifier;
                break;
            case < 3:
                Time.timeScale += 0.01f + modifier;
                break;
            default:
                Time.timeScale = 1f;
                break;
        }
        
        Time.timeScale = Mathf.Clamp(Time.timeScale, 0.8f, 1.2f);
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
        _accumulatedDeltaTime += Time.deltaTime * _deltaTimeMultiplier;
        var canonicalPosition = entityInterpolation.GetCanonicalPosition();
        
        while (_accumulatedDeltaTime >= _serverUpdateInterval)
        {
            _updateRateCache.AddToStream(
                ClientUpdateRateKey,
                new UpdateRateCache.Entry
                {
                    Rate = (DateTimeOffset.Now - _lastClientUpdateAt).TotalMilliseconds,
                    SequenceId = _currentSequenceId
                }
            );
            
            _lastClientUpdateAt = DateTimeOffset.Now;
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
        GameManager.Conn.Reducers
            .UpdatePlayerInput(new Input(_currentSequenceId, direction));
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