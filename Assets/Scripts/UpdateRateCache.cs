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
    
    public UpdateRateCache(int capacity, string[] streamIds)
    {
        Capacity = capacity;
        Streams = new Dictionary<string, List<Entry>>();
        streamIds.ToList().ForEach(streamId => Streams.Add(streamId, new List<Entry>()));
    }
    
    public Dictionary<string, List<Entry>> Streams { get; }

    public void AddToStream(string key, Entry value)
    {
        if (!Streams.TryGetValue(key, out var stream))
        {
            return;
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

    public void Unsubscribe(ISubscriber<UpdateRateCache> subscriber)
    {
        var containsSubscriber = _updateRateSubscribers.Contains(subscriber);
        if(containsSubscriber) _updateRateSubscribers.Remove(subscriber);
    }
}