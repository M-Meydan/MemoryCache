## MemoryCache<TValue>

The `MemoryCache<TValue>` class is a custom cache implementation that uses the Least Recently Used (LRU) eviction strategy. 
It provides an in-memory cache for storing key-value pairs with a fixed capacity.
The cache ensures that the most recently used items are retained while evicting the least recently used items when the cache reaches its capacity.

## Features

- **Flexible and Generic**: The `MemoryCache` component can store and retrieve arbitrary types of objects.
- **Configurable Capacity**: You can set a maximum capacity for the cache to limit the number of items it can hold at any given time.
- **LRU Eviction Strategy**: When the cache reaches its maximum capacity, new items added to the cache will result in the eviction of the least recently used item.
- **Thread-Safe**: The `MemoryCache` is designed to be thread-safe, allowing concurrent access to cache operations from multiple threads.
- **Item Eviction Event**: You can subscribe to the `ItemEvictedAsync` event to receive notifications when items are evicted from the cache.

### Key Methods

- `AddOrUpdate(string key, TValue value)`: Adds a new item or updates an existing item in the cache. If the cache already contains the item, it updates its value and moves it to the end of the access order. If the cache is at capacity, it removes the least recently used item before adding the new item.
- `Get(string key)`: Retrieves a cache item by key. If the item exists, it moves the item to the end of the access order to mark it as the most recently used. If the item is not found, it returns the default value of `TValue`.

### Usage

You can easily integrate the `MemoryCache` component as a singleton dependency. Here's an example:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register the MemoryCache as a singleton service
    services.AddSingleton<IMemoryCache<string>, MemoryCache<string>>(provider =>
    {
        var capacity = 2; // Set the desired cache capacity
        return new MemoryCache<string>(capacity);
    });
}
```

### Example

```csharp
// var capacity = 2;
var cache = serviceProvider.GetService<IMemoryCache<string>>();
cache.ItemEvictedAsync += async (key) =>
{
    Console.WriteLine($"Item with key '{key}' evicted from the cache.");
};

cache.AddOrUpdate("key1", "value1");
cache.AddOrUpdate("key2", "value2");
cache.AddOrUpdate("key3", "value3"); //Item with key 'key1' evicted from the cache.

var value1 = cache.Get("key1");
var value2 = cache.Get("key2");
var value3 = cache.Get("key3");

Console.WriteLine(value1); // Output: null
Console.WriteLine(value2); // Output: "value2"
Console.WriteLine(value3); // Output: "value3"
```

Unit tests included.
