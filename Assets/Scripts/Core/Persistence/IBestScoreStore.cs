namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Interface for storing best scores.
    /// </summary>
    public interface IBestScoreStore
    {
        int GetBestScore();
        void SetBestScore(int score);
    }
    
    /// <summary>
    /// Simple in-memory best score store.
    /// </summary>
    public class InMemoryBestScoreStore : IBestScoreStore
    {
        private int _bestScore = 0;
        
        public int GetBestScore() => _bestScore;
        public void SetBestScore(int score) => _bestScore = score;
    }
}