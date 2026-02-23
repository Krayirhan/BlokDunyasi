namespace BlockPuzzle.UnityAdapter.Boot
{
    public enum GameLaunchMode
    {
        Auto,
        NewGame,
        Continue
    }

    public static class GameLaunchState
    {
        public static GameLaunchMode LaunchMode { get; private set; } = GameLaunchMode.Auto;

        public static void RequestNewGame()
        {
            LaunchMode = GameLaunchMode.NewGame;
        }

        public static void RequestContinue()
        {
            LaunchMode = GameLaunchMode.Continue;
        }

        public static void Reset()
        {
            LaunchMode = GameLaunchMode.Auto;
        }
    }
}
