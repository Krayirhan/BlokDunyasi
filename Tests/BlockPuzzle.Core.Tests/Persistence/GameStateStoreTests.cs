using System;
using System.Collections.Generic;
using BlockPuzzle.Core.Board;
using BlockPuzzle.Core.Persistence;
using NUnit.Framework;

namespace BlockPuzzle.Core.Tests.Persistence
{
    [TestFixture]
    [Category("Unit")]
    public class GameStateStoreTests
    {
        [Test]
        public void LoadGame_MigratesLegacySaveToCurrentVersion()
        {
            var storage = new InMemoryStorageProvider();
            var serializer = new StubJsonSerializer();
            var store = new GameStateStore(storage, serializer);

            serializer.DataToReturn = new GameData
            {
                SaveVersion = 1,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = new CellState[16],
                Score = 77,
                ActiveBlocks = new[] { new Shapes.ShapeId(1) }
            };

            storage.SaveString("legacy", "{legacy}");

            var loaded = store.LoadGame("legacy");

            Assert.NotNull(loaded);
            Assert.AreEqual(GameData.CurrentSaveVersion, loaded.SaveVersion);
            Assert.AreEqual(77, loaded.Score);
            CollectionAssert.AreEqual(new[] { 1, -1, -1 }, loaded.ActiveBlockSlots);
        }

        [Test]
        public void LoadGame_FutureVersionSave_ReturnsNull()
        {
            var storage = new InMemoryStorageProvider();
            var serializer = new StubJsonSerializer();
            var store = new GameStateStore(storage, serializer);

            serializer.DataToReturn = new GameData
            {
                SaveVersion = GameData.CurrentSaveVersion + 1,
                BoardWidth = 4,
                BoardHeight = 4,
                BoardCells = new CellState[16]
            };

            storage.SaveString("future", "{future}");

            var loaded = store.LoadGame("future");

            Assert.IsNull(loaded);
        }

        private sealed class InMemoryStorageProvider : IStorageProvider
        {
            private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);

            public string LoadString(string key)
            {
                return _strings.TryGetValue(key, out var value) ? value : string.Empty;
            }

            public void SaveString(string key, string value)
            {
                _strings[key] = value;
            }

            public int LoadInt(string key, int defaultValue = 0)
            {
                return defaultValue;
            }

            public void SaveInt(string key, int value)
            {
            }

            public bool HasKey(string key)
            {
                return _strings.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _strings.Remove(key);
            }

            public void Save()
            {
            }
        }

        private sealed class StubJsonSerializer : IJsonSerializer
        {
            public GameData DataToReturn { get; set; }

            public string Serialize<T>(T obj)
            {
                return "{serialized}";
            }

            public T Deserialize<T>(string json)
            {
                return (T)(object)DataToReturn;
            }
        }
    }
}
