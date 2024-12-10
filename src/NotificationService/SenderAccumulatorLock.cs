namespace NotificationService
{
    public class SenderAccumulatorLock
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
    }
}
