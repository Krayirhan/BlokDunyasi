using System;

namespace BlockPuzzle.Core.Rules
{
    /// <summary>
    /// Rounding behavior used when converting raw score to integer score delta.
    /// </summary>
    public enum ScoreRoundingMode
    {
        Nearest = 0,
        Floor = 1,
        Ceiling = 2,
        Truncate = 3
    }

    /// <summary>
    /// A point on a score multiplier curve.
    /// </summary>
    [Serializable]
    public readonly struct ScoreCurvePoint
    {
        public readonly int X;
        public readonly float Y;

        public ScoreCurvePoint(int x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Data-driven score formula configuration.
    /// </summary>
    [Serializable]
    public sealed class ScoreConfig
    {
        public const int DefaultFormulaVersion = 1;

        public int FormulaVersion { get; }
        public int BasePointsPerLine { get; }
        public ScoreRoundingMode RoundingMode { get; }
        public ScoreCurvePoint[] LineMultiplierCurve { get; }
        public ScoreCurvePoint[] ComboMultiplierCurve { get; }

        public static ScoreConfig Default { get; } = CreateDefault();

        public ScoreConfig(
            int formulaVersion,
            int basePointsPerLine,
            ScoreRoundingMode roundingMode,
            ScoreCurvePoint[] lineMultiplierCurve,
            ScoreCurvePoint[] comboMultiplierCurve)
        {
            if (formulaVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(formulaVersion), "Formula version must be positive.");
            if (basePointsPerLine < 0)
                throw new ArgumentOutOfRangeException(nameof(basePointsPerLine), "Base points cannot be negative.");
            if (lineMultiplierCurve == null || lineMultiplierCurve.Length == 0)
                throw new ArgumentException("Line multiplier curve must contain at least one point.", nameof(lineMultiplierCurve));
            if (comboMultiplierCurve == null || comboMultiplierCurve.Length == 0)
                throw new ArgumentException("Combo multiplier curve must contain at least one point.", nameof(comboMultiplierCurve));

            FormulaVersion = formulaVersion;
            BasePointsPerLine = basePointsPerLine;
            RoundingMode = roundingMode;
            LineMultiplierCurve = CloneAndSort(lineMultiplierCurve);
            ComboMultiplierCurve = CloneAndSort(comboMultiplierCurve);
        }

        public float EvaluateLineMultiplier(int linesCleared)
        {
            if (linesCleared <= 0)
                return 1.0f;

            return EvaluateCurve(LineMultiplierCurve, linesCleared);
        }

        public float EvaluateComboMultiplier(int comboStreak)
        {
            if (comboStreak <= 0)
                return 1.0f;

            return EvaluateCurve(ComboMultiplierCurve, comboStreak);
        }

        private static ScoreCurvePoint[] CloneAndSort(ScoreCurvePoint[] source)
        {
            var clone = (ScoreCurvePoint[])source.Clone();
            Array.Sort(clone, (a, b) => a.X.CompareTo(b.X));
            return clone;
        }

        private static float EvaluateCurve(ScoreCurvePoint[] curve, int x)
        {
            if (curve == null || curve.Length == 0)
                return 1.0f;

            if (curve.Length == 1)
                return curve[0].Y;

            if (x <= curve[0].X)
                return Interpolate(curve[0], curve[1], x);

            for (int i = 0; i < curve.Length - 1; i++)
            {
                var left = curve[i];
                var right = curve[i + 1];
                if (x <= right.X)
                    return Interpolate(left, right, x);
            }

            return Interpolate(curve[curve.Length - 2], curve[curve.Length - 1], x);
        }

        private static float Interpolate(ScoreCurvePoint left, ScoreCurvePoint right, int x)
        {
            if (left.X == right.X)
                return right.Y;

            float t = (x - left.X) / (float)(right.X - left.X);
            return left.Y + (right.Y - left.Y) * t;
        }

        private static ScoreConfig CreateDefault()
        {
            return new ScoreConfig(
                formulaVersion: DefaultFormulaVersion,
                basePointsPerLine: 10,
                roundingMode: ScoreRoundingMode.Nearest,
                lineMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.0f),
                    new ScoreCurvePoint(2, 1.5f)
                },
                comboMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.0f),
                    new ScoreCurvePoint(2, 1.1f)
                });
        }
    }
}
