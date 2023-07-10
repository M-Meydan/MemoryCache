namespace MemoryCache
{
    /// <summary>
    /// Custom cache with LRU eviction strategy
    /// </summary>
    public class MemoryCache<TValue> : IMemoryCache<TValue> 
    {
        readonly int _capacity;
        readonly Dictionary<string, LinkedListNode<KeyValuePair<string, TValue>>> _cache;
        readonly LinkedList<KeyValuePair<string, TValue>> _orderedItems;
        readonly ReaderWriterLockSlim _cacheLock;

        public MemoryCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            _capacity = capacity;
            _cache = new Dictionary<string, LinkedListNode<KeyValuePair<string, TValue>>>(capacity);
            _orderedItems = new LinkedList<KeyValuePair<string, TValue>>();
            _cacheLock = new ReaderWriterLockSlim();
        }

        public event Func<string, Task> ItemEvictedAsync;

        /// <summary>
        /// Adds new or updates an existing cache item
        /// </summary>
        public void AddOrUpdate(string key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _cacheLock.EnterWriteLock();

            try
            {
                if (ExistingCacheItemHandled(key, value))
                    return;

                if (IsMaxSizeReached())
                    RemoveLeastRecentlyUsedItem();

                _cache[key] = _orderedItems.AddLast(new KeyValuePair<string, TValue>(key, value));
            }
            finally { _cacheLock.ExitWriteLock(); }
        }

        /// <summary>
        /// Retrieves a cache item by key
        /// </summary>
        public TValue? Get(string key)
        {
            _cacheLock.EnterUpgradeableReadLock();

            try
            {
                if (key!=null && _cache.TryGetValue(key, out var node))
                {
                    _cacheLock.EnterWriteLock();

                    try { MoveNodeToLast(node); }
                    finally { _cacheLock.ExitWriteLock(); }

                    return node.Value.Value;
                }

                return default(TValue);
            }
            finally { _cacheLock.ExitUpgradeableReadLock(); }
        }

        public void Dispose()
        {
            _cacheLock.Dispose();
        }

        #region Private methods

        /// <summary>
        /// If cache item exists updates its value and moves it to the last ordering.
        /// </summary>
        /// <returns>true: if cache item exists otherwise false</returns>
        private bool ExistingCacheItemHandled(string key, TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                node.Value = new KeyValuePair<string, TValue>(key, value);
                MoveNodeToLast(node);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove the least recently used item (the first node in the linked list)
        /// </summary>
        private void RemoveLeastRecentlyUsedItem()
        {
            var leastRecentlyUsedNode = _orderedItems.First;
            var key = leastRecentlyUsedNode.Value.Key;
            _cache.Remove(key);
            _orderedItems.RemoveFirst();

            // Invoke the ItemEvicted event asynchronously on a background thread
            if (ItemEvictedAsync != null)
                _ = Task.Run(() => ItemEvictedAsync?.Invoke(key));
        }


        private void MoveNodeToLast(LinkedListNode<KeyValuePair<string, TValue>> node)
        {
            if (node != _orderedItems.Last)
            {
                _orderedItems.Remove(node);
                _orderedItems.AddLast(node);
            }
        }

        private bool IsMaxSizeReached()
        {
            return _cache.Count == _capacity;
        }

        #endregion
    }

}
