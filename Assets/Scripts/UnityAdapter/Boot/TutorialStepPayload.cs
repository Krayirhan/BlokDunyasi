namespace BlockPuzzle.UnityAdapter.Boot
{
    public readonly struct TutorialStepPayload
    {
        public readonly bool Visible;
        public readonly int StepIndex;
        public readonly int TotalSteps;
        public readonly string Title;
        public readonly string Description;

        public TutorialStepPayload(bool visible, int stepIndex, int totalSteps, string title, string description)
        {
            Visible = visible;
            StepIndex = stepIndex;
            TotalSteps = totalSteps;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
