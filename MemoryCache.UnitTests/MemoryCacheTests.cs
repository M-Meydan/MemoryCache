using System.Diagnostics;

namespace MemoryCache.UnitTests
{
    [TestFixture]
    public class MemoryCacheTests
    {
        MemoryCache<string> _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new MemoryCache<string>(10);
        }

        [TearDown]
        public void Dispose()
        {
            _cache.Dispose();
        }

        [Test]
        public void AddOrUpdate_NewItem_CacheNotAtCapacity_AddsItem()
        {
            // Act
            _cache.AddOrUpdate("one", "1");

            // Assert
            var value = _cache.Get("one");
            Assert.AreEqual("1", value);
        }

        [Test]
        public void AddOrUpdate_ExistingItem_UpdatesValue()
        {
            // Arrange
            _cache.AddOrUpdate("one", "1");

            // Act
            _cache.AddOrUpdate("one", "10");

            // Assert
            var value = _cache.Get("one");
            Assert.AreEqual("10", value);
        }

        [Test]
        public void AddOrUpdate_NewItem_CacheAtCapacity_RemovesLeastRecentlyUsedItem()
        {
            // Arrange
            var cache = new MemoryCache<int>(2);
            cache.AddOrUpdate("one", 1);
            cache.AddOrUpdate("two", 2);

            // Act
            cache.AddOrUpdate("three", 3); // "one"should be removed

            cache.Get(null);

            // Assert
            var valueOne = cache.Get("one");
            var valueTwo = cache.Get("two");
            var valueThree = cache.Get("three");

            Assert.AreEqual(default(int), valueOne); // returns default as "one" not exist anymore
            Assert.AreEqual(2, valueTwo);
            Assert.AreEqual(3, valueThree);

            cache.Dispose();
        }

        [Test]
        public void AddOrUpdate_NullKey_ThrowsException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => _cache.AddOrUpdate(null, "Value"));
        }


        [Test]
        public void Get_ReturnsCorrectValues()
        {
            // Act
            _cache.AddOrUpdate("Key1", "Value1");
            _cache.AddOrUpdate("Key2", "Value2");
            _cache.AddOrUpdate("Key3", "Value3");

            var value1 = _cache.Get("Key1");
            var value2 = _cache.Get("Key2");
            var value3 = _cache.Get("Key3");
            var value4 = _cache.Get("Key4");
            var @null = _cache.Get(null);

            // Assert
            Assert.AreEqual("Value1", value1);
            Assert.AreEqual("Value2", value2);
            Assert.AreEqual("Value3", value3);
            Assert.IsNull(value4);
            Assert.IsNull(@null);
        }

        [Test]
        public void Get_KeyNotFound_ReturnsDefaultValue()
        {
            // Act
            var value = _cache.Get("one");

            // Assert
            Assert.AreEqual(default(string), value);
        }


        [Test]
        [Repeat(3)]
        public void ConcurrentAccess_WithEvictionPolicy_ReturnsCorrectValues()
        {
            // Arrange
            var cache = new MemoryCache<string>(2); // Set a small cache size for testing
            var tasks = new List<Task>();

            cache.AddOrUpdate("0", "value0");
            cache.AddOrUpdate("1", "value1");

            // Concurrent Add/Update/Get tasks
            var addOrUpdateGetTasks = new[]
            {   Task.Run(() => { cache.Get("0"); }),
                Task.Run(() => { cache.AddOrUpdate("3", "value3"); }),
                Task.Run(() => { cache.AddOrUpdate("4", "value4"); }),
                Task.Run(() => { cache.Get("1"); }),
                Task.Run(() => { cache.Get("2"); }),
                Task.Run(() => { cache.Get("3"); }),
                Task.Run(() => { cache.Get("4"); }),
                Task.Run(() => { cache.AddOrUpdate("4", "updated4"); }),
                Task.Run(() => { cache.AddOrUpdate("3", "updated3"); }),
            };

            // Await all Add/Update/Get tasks
            Task.WaitAll(addOrUpdateGetTasks);

            // Assert - Verify correct values
            Assert.IsNull(cache.Get("0"));
            Assert.IsNull(cache.Get("1"));
            Assert.IsNull(cache.Get("2"));
            Assert.IsNotNull(cache.Get("3"));
            Assert.IsNotNull(cache.Get("4"));

            cache.Dispose();
        }

        [Test]
        public async Task ItemEvictedAsync_EventIsRaised()
        {
            // Arrange
            var cache = new MemoryCache<string>(2);
            var evictedKey = string.Empty;

            cache.ItemEvictedAsync += async key =>
            {
                evictedKey = key;
            };

            // Act
            cache.AddOrUpdate("1", "Value 1");// This will get evicted
            cache.AddOrUpdate("2", "Value 2");
            cache.AddOrUpdate("3", "Value 3");// This will trigger eviction

            // Wait for the ItemEvictedAsync to complete
            await Task.Delay(100);

            // Assert
            Assert.That(evictedKey, Is.EqualTo("1")); // Verify that key 1 was evicted
            cache.Dispose();
        }

    }
}