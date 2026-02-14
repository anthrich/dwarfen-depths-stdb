using System;
using System.Collections.Generic;
using SharedPhysics;
using UnityEngine;
using Entity = SharedPhysics.Entity;
using Input = SpacetimeDB.Types.Input;
using Vector2 = SharedPhysics.Vector2;

public class Simulation : MonoBehaviour, IPublisher<Entity>
{
    private float _serverUpdateInterval = 0.05f;
    private float _accumulatedDeltaTime;
    private ulong _currentSequenceId;
    private const int CacheSize = 1024;
    
    private const string ServerUpdateRateKey = "server-update-rate";
    private const string ClientUpdateRateKey = "client-update-rate";
    private DateTimeOffset _lastServerUpdateAt;
    private DateTimeOffset _lastClientUpdateAt;
    
    private UnityEngine.Vector2 _inputDirection;
    private float _inputYRotation;
    private uint _targetId;
    private readonly Entity[] _simulationStateCache = new Entity[CacheSize];
    private Entity _localPlayerEntity;
    
    private Entity _serverEntityState;
    private readonly List<Entity> _entities = new();
    private List<Line> _lines = new();
    private readonly List<Input> _inputsAheadOfSimulation = new();
    private ulong _lastCorrectedSequenceId;
    private List<ISubscriber<Entity>> _subscribers = new();
    private readonly UpdateRateCache _updateRateCache = new(30, new []{ ServerUpdateRateKey, ClientUpdateRateKey });
    
    public static Simulation Instance { get; private set; }

    public void Start()
    {
        Instance = this;
    }

    public void Init(string mapName)
    {
        _serverUpdateInterval = GameManager.Config.UpdateEntityInterval;
        _lines = new List<Line>(MapData.GetMap(mapName).Lines);
    }

    public void SetLocalPlayerEntity(Entity localPlayerEntity)
    {
        _localPlayerEntity = localPlayerEntity;
        _currentSequenceId = localPlayerEntity.SequenceId;
        _lastServerUpdateAt = DateTimeOffset.Now;
        _lastClientUpdateAt = DateTimeOffset.Now;
    }

    public void SetInputDirection(UnityEngine.Vector2 direction)
    {
        _inputDirection = direction;
    }

    public void SetInputRotation(float yRotation)
    {
        _inputYRotation = yRotation;
    }

    public void SetTarget(EntityController target)
    {
        _targetId = target.entityId;
    }
    
    public void OnEntityUpdated(Entity newServerEntityState)
    {
        if(newServerEntityState.Id != _localPlayerEntity.Id) return;
        if(newServerEntityState.SequenceId <= _serverEntityState.SequenceId) return;
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
    }
    
    private void Update()
    {
        if(_currentSequenceId == 0) return;
        _accumulatedDeltaTime += Time.deltaTime;
        
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
            var input = new InputState
            {
                Direction = _inputDirection,
                SequenceId = _currentSequenceId,
                TargetEntityId = _targetId,
                YRotation = _inputYRotation
            };
            _localPlayerEntity.Direction = _inputDirection.ToSharedPhysicsV2();
            _localPlayerEntity.Rotation = _inputYRotation;
            
            var result = Engine.Simulate(
                _serverUpdateInterval,
                _currentSequenceId,
                new[] { _localPlayerEntity },
                _lines.ToArray()
            );
            
            _localPlayerEntity = result[0];
            var cacheIndex = Convert.ToInt32(_currentSequenceId % CacheSize);
            _simulationStateCache[cacheIndex] = _localPlayerEntity;
            
            _entities.Clear();
            _entities.AddRange(result);
            SendInput(input);
            _currentSequenceId++;
        }
        
        _localPlayerEntity = Reconcile(_localPlayerEntity);
        
        foreach (var subscriber in _subscribers)
        {
            subscriber.SubscriptionUpdate(_localPlayerEntity);
        }
    }
    
    private Entity Reconcile(Entity localPlayerEntity)
    {
        if(_serverEntityState.SequenceId <= _lastCorrectedSequenceId) return localPlayerEntity;
        var cacheIndex = Convert.ToInt32(_serverEntityState.SequenceId % CacheSize);
        var cachedSimulationState = _simulationStateCache[cacheIndex];
        var position = _serverEntityState.Position;
        var posDif = Vector2.Distance(cachedSimulationState.Position, position);
        
        if (posDif > 0.001f)
        {
            localPlayerEntity = _serverEntityState;
            var rewindTick = _serverEntityState.SequenceId + 1;
            
            while (rewindTick < _currentSequenceId)
            {
                var rewindCacheIndex = Convert.ToInt32(rewindTick % CacheSize);
                var rewoundSimulation = _simulationStateCache[rewindCacheIndex];
                
                if (rewoundSimulation.SequenceId != rewindTick)
                {
                    ++rewindTick;
                    continue;
                }
                
                var result = Engine.Simulate(
                    _serverUpdateInterval,
                    rewindTick,
                    new [] {
                        localPlayerEntity
                    },
                    _lines.ToArray()
                );
                localPlayerEntity = result[0];
                _simulationStateCache[rewindCacheIndex] = localPlayerEntity;
                ++rewindTick;
            }
        }
        
        _lastCorrectedSequenceId = _serverEntityState.SequenceId;
        
        return localPlayerEntity;
    }
    
    private void SendInput(InputState inputState)
    {
        _inputsAheadOfSimulation.Add(
            new Input(
                inputState.SequenceId,
                inputState.Direction.ToDbVector2(),
                inputState.YRotation,
                inputState.TargetEntityId
            )
        );
        if(_inputsAheadOfSimulation.Count > 12) _inputsAheadOfSimulation.RemoveAt(0);
        GameManager.SendInput(_inputsAheadOfSimulation);
    }

    public void Subscribe(ISubscriber<Entity> subscriber)
    {
        _subscribers.Add(subscriber);
    }

    public void Unsubscribe(ISubscriber<Entity> subscriber)
    {
        _subscribers.Remove(subscriber);
    }
    
    public void Subscribe(ISubscriber<UpdateRateCache> subscriber)
    {
        _updateRateCache.Subscribe(subscriber);
    }

    public void Unsubscribe(ISubscriber<UpdateRateCache> subscriber)
    {
        _updateRateCache.Unsubscribe(subscriber);
    }
}