public interface IPublisher<T>
{
    void Subscribe(ISubscriber<T> subscriber);
    void Unsubscribe(int instanceId);
}

public interface ISubscriber<T>
{
    public int GetInstanceID();
    void SubscriptionUpdate(T message);
}