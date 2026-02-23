// File: Core/Persistence/IJsonSerializer.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Interface for JSON serialization.
    /// Abstracts away serialization implementation (JsonUtility, Newtonsoft, etc.).
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes an object to JSON string.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>JSON string representation</returns>
        string Serialize<T>(T obj);
        
        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>Deserialized object</returns>
        T Deserialize<T>(string json);
    }
}
