// File: Core/Persistence/IStorageProvider.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Interface for persistent storage operations.
    /// Abstracts away storage implementation (PlayerPrefs, files, cloud, etc.).
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Loads a string value from storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <returns>Stored value, or empty string if not found</returns>
        string LoadString(string key);
        
        /// <summary>
        /// Saves a string value to storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="value">Value to save</param>
        void SaveString(string key, string value);
        
        /// <summary>
        /// Loads an integer value from storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Stored value, or defaultValue if not found</returns>
        int LoadInt(string key, int defaultValue = 0);
        
        /// <summary>
        /// Saves an integer value to storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="value">Value to save</param>
        void SaveInt(string key, int value);
        
        /// <summary>
        /// Checks if a key exists in storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <returns>True if key exists</returns>
        bool HasKey(string key);
        
        /// <summary>
        /// Deletes a key from storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        void DeleteKey(string key);
        
        /// <summary>
        /// Flushes any pending writes to storage.
        /// </summary>
        void Save();
    }
}
