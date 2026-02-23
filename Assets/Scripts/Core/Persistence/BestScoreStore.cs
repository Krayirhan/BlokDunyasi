// File: Core/Persistence/BestScoreStore.cs
namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Persistent storage for best score using IStorageProvider.
    /// </summary>
    public sealed class BestScoreStore : IBestScoreStore
    {
        private const string BEST_SCORE_KEY = "BlokDunyasi_BestScore";
        private readonly IStorageProvider _storage;
        private int _cachedBestScore;
        private bool _cacheValid;
        
        public BestScoreStore(IStorageProvider storage)
        {
            _storage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            _cacheValid = false;
        }
        
        public int GetBestScore()
        {
            if (!_cacheValid)
            {
                _cachedBestScore = _storage.LoadInt(BEST_SCORE_KEY, 0);
                _cacheValid = true;
            }
            return _cachedBestScore;
        }
        
        public void SetBestScore(int score)
        {
            if (score > _cachedBestScore || !_cacheValid)
            {
                _cachedBestScore = score;
                _cacheValid = true;
                _storage.SaveInt(BEST_SCORE_KEY, score);
                _storage.Save();
            }
        }
        
        public bool TryUpdateBestScore(int newScore)
        {
            if (newScore > GetBestScore())
            {
                SetBestScore(newScore);
                return true;
            }
            return false;
        }
        
        public void Reset()
        {
            _cachedBestScore = 0;
            _cacheValid = true;
            _storage.DeleteKey(BEST_SCORE_KEY);
            _storage.Save();
        }
    }
}
