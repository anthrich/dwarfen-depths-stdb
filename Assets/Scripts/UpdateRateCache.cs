using System.Collections.Generic;
using System.Linq;

public class UpdateRateCache : IPublisher<UpdateRateCache>
{
    public readonly int Capacity;
    private readonly List<ISubscriber<UpdateRateCache>> _updateRateSubscribers = new();
    
    public struct Entry
    {
        public double Rate;
        public ulong SequenceId;
    }
    
    public UpdateRateCache(int capacity)
    {
        Capacity = capacity;
        Streams = new Dictionary<string, List<Entry>>();
    }
    
    public Dictionary<string, List<Entry>> Streams { get; }

    public void AddToStream(string key, Entry value)
    {
        if (!Streams.TryGetValue(key, out var stream))
        {
            Streams.Add(key, new List<Entry>());
            stream = Streams[key];
        }
        
        stream.Add(value);
        if(stream.Count > Capacity) stream.RemoveAt(0);

        foreach (var updateRateSubscriber in _updateRateSubscribers)
        {
            updateRateSubscriber.SubscriptionUpdate(this);
        }
    }

    public void Subscribe(ISubscriber<UpdateRateCache> subscriber)
    {
        _updateRateSubscribers.Add(subscriber);
    }

    public void Unsubscribe(int instanceId)
    {
        var subscriber = _updateRateSubscribers.FirstOrDefault(s => s.GetInstanceID() == instanceId);
        if(subscriber != null) _updateRateSubscribers.Remove(subscriber);
    }
}