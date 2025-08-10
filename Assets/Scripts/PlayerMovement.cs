using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SharedPhysics;
using UnityEngine;
using UnityEngine.InputSystem;
using Entity = SpacetimeDB.Types.Entity;
using Input = SpacetimeDB.Types.Input;
using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(EntityInterpolation))]
[RequireComponent(typeof(EntityAnimator))]
public class PlayerMovement :
    MonoBehaviour,
    IPublisher<UpdateRateCache>,
    IPublisher<InputState>
{
    public Transform cameraTransform;
    public EntityInterpolation entityInterpolation;
    public EntityAnimator entityAnimator;
    public Transform serverStateObject;

    private const string ServerUpdateRateKey = "server-update-rate";
    private const string ClientUpdateRateKey = "client-update-rate";
    private const int CacheSize = 1024;
    
    private float _serverUpdateInterval;
    private float MovementSpeed => _serverEntityState.Speed;
    private float SpeedPerInterval => MovementSpeed * _serverUpdateInterval;
    private Vector2 _movementInput = Vector2.zero;
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
    private readonly List<Input> _inputsAheadOfSimulation = new();
    private readonly UpdateRateCache _updateRateCache = new(30, new []{ ServerUpdateRateKey, ClientUpdateRateKey });
    private readonly List<ISubscriber<InputState>> _inputStateSubscribers = new();

    void Start()
    {
        if(cameraTransform == default) cameraTransform = Camera.main?.transform ?? transform;
        if(entityInterpolation == default) entityInterpolation = GetComponent<EntityInterpolation>();
        if(!entityAnimator) entityAnimator = GetComponent<EntityAnimator>();
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

    public void Unsubscribe(ISubscriber<UpdateRateCache> subscriber)
    {
        _updateRateCache.Unsubscribe(subscriber);
    }
    
    public void Subscribe(ISubscriber<InputState> subscriber)
    {
        _inputStateSubscribers.Add(subscriber);
    }

    public void Unsubscribe(ISubscriber<InputState> subscriber)
    {
        if(_inputStateSubscribers.Contains(subscriber)) _inputStateSubscribers.Remove(subscriber);
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
        _inputsAheadOfSimulation.RemoveAll(i => i.SequenceId <= _serverEntityState.SequenceId);
        serverStateObject.position = newServerEntityState.Position.ToGamePosition(_yPosition);
    }

    [UsedImplicitly]
    private void OnMove(InputValue value)
    {
        var newInput = value.Get<Vector2>();
        if(newInput.ApproximatesTo(_movementInput)) return;
        _movementInput = newInput;
        TransformMovementWithCamera();
    }

    [UsedImplicitly]
    private void OnLookApplied()
    {
        TransformMovementWithCamera();
    }

    private void TransformMovementWithCamera()
    {
        var transformedMovement = cameraTransform.TransformDirection(_movementInput.ToGamePosition(_yPosition));
        transformedMovement.y = 0;
        transformedMovement = transformedMovement.normalized;
        _movement = new Vector2(transformedMovement.x, transformedMovement.z);
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
            var result = Engine.Simulate(
                _serverUpdateInterval,
                _currentSequenceId,
                new [] {
                    new SharedPhysics.Entity
                    {
                        Id = _serverEntityState.EntityId,
                        Position = canonicalPosition.ToSharedPhysicsV2(),
                        Direction = input.Direction.ToSharedPhysicsV2(),
                        SequenceId = _currentSequenceId,
                        Speed = _serverEntityState.Speed
                    }
                },
                Array.Empty<Line>()
            );
            canonicalPosition = result[0].Position.ToGamePosition(_yPosition);
            SendInput(input);
            _currentSequenceId++;
        }
        
        canonicalPosition = Reconcile(canonicalPosition);
        
        entityInterpolation.SetCanonicalPosition(canonicalPosition);
        entityInterpolation.SetMovementDirection(_movement.ToGamePosition(canonicalPosition.y));
        entityAnimator.SetDirection(_movement);
    }
    
    private Vector3 ApplyDirection(Vector2 direction, Vector3 targetPosition)
    {
        var movement = direction * SpeedPerInterval;
        return targetPosition + new Vector3(movement.x, 0, movement.y);
    }

    private void SendInput(InputState inputState)
    {
        _inputsAheadOfSimulation.Add(new Input(inputState.SequenceId, inputState.Direction.ToDbVector2()));
        GameManager.Conn.Reducers.UpdatePlayerInput(_inputsAheadOfSimulation);
        foreach (var inputStateSubscriber in _inputStateSubscribers)
        {
            inputStateSubscriber.SubscriptionUpdate(inputState);
        }
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
                
                canonicalPosition = ApplyDirection(rewoundInput.Direction, canonicalPosition);
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