using BlockPuzzle.Core.Rules;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Migration result for score formula version changes in saved games.
    /// </summary>
    public readonly struct ScoreFormulaMigrationResult
    {
        public readonly int SourceVersion;
        public readonly int TargetVersion;
        public readonly bool Migrated;
        public readonly string Note;

        public ScoreFormulaMigrationResult(int sourceVersion, int targetVersion, bool migrated, string note)
        {
            SourceVersion = sourceVersion;
            TargetVersion = targetVersion;
            Migrated = migrated;
            Note = note;
        }
    }

    /// <summary>
    /// Applies score formula migration logic to loaded save data.
    /// </summary>
    public static class ScoreFormulaMigration
    {
        public static ScoreFormulaMigrationResult MigrateInPlace(GameData data, int targetFormulaVersion)
        {
            if (data == null)
                throw new System.ArgumentNullException(nameof(data));

            int sourceVersion = data.ScoreFormulaVersion <= 0
                ? ScoreConfig.DefaultFormulaVersion
                : data.ScoreFormulaVersion;

            int targetVersion = targetFormulaVersion <= 0
                ? ScoreConfig.DefaultFormulaVersion
                : targetFormulaVersion;

            bool sanitized = false;
            if (data.Score < 0)
            {
                data.Score = 0;
                sanitized = true;
            }

            if (data.ComboStreak < 0)
            {
                data.ComboStreak = 0;
                sanitized = true;
            }

            bool versionChanged = sourceVersion != targetVersion;
            data.ScoreFormulaVersion = targetVersion;

            if (!versionChanged && !sanitized)
            {
                return new ScoreFormulaMigrationResult(
                    sourceVersion,
                    targetVersion,
                    migrated: false,
                    note: "No score migration required.");
            }

            string note = versionChanged
                ? $"Score formula migrated from v{sourceVersion} to v{targetVersion} (score preserved)."
                : $"Score data sanitized for formula v{targetVersion}.";

            return new ScoreFormulaMigrationResult(
                sourceVersion,
                targetVersion,
                migrated: true,
                note: note);
        }
    }
}
