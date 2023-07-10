namespace MemoryCache
{
    public interface IMemoryCache<TValue>
    {
        TValue? Get(string key);
        void AddOrUpdate(string key, TValue value);
        void Dispose();
        event Func<string, Task> ItemEvictedAsync;
    }
}