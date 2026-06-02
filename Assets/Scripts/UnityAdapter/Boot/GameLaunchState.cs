namespace BlockPuzzle.UnityAdapter.Boot
{
    public enum GameMode
    {
        Classic = 0,
        Challenge = 1,
        Zen = 2
    }

    public enum GameLaunchMode
    {
        Auto,
        NewGame,
        Continue
    }

    public static class GameLaunchState
    {
        public static GameLaunchMode LaunchMode { get; private set; } = GameLaunchMode.Auto;
        public static GameMode SelectedMode { get; private set; } = GameMode.Classic;
        public static bool ForceTutorialReplay { get; private set; }

        public static void RequestNewGame(GameMode mode = GameMode.Classic)
        {
            LaunchMode = GameLaunchMode.NewGame;
            SelectedMode = mode;
            ForceTutorialReplay = false;
        }

        public static void RequestTutorialReplay(GameMode mode = GameMode.Classic)
        {
            LaunchMode = GameLaunchMode.NewGame;
            SelectedMode = mode;
            ForceTutorialReplay = false;
        }

        public static void RequestContinue()
        {
            LaunchMode = GameLaunchMode.Continue;
            ForceTutorialReplay = false;
        }

        public static void Reset()
        {
            LaunchMode = GameLaunchMode.Auto;
            SelectedMode = GameMode.Classic;
            ForceTutorialReplay = false;
        }
    }
}
