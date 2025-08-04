public interface IPublisher<T>
{
    void Subscribe(ISubscriber<T> subscriber);
    void Unsubscribe(ISubscriber<T> subscriber);
}

public interface ISubscriber<T>
{
    public int GetInstanceID();
    void SubscriptionUpdate(T update);
}