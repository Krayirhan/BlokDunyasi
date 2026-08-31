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
        public const int DefaultFormulaVersion = 11;

        public int FormulaVersion { get; }
        public int BasePointsPerLine { get; }
        public int BasePointsPerPlacement { get; }
        public int BasePointsPerPlacedCell { get; }
        public int HighRiskPlacementBonus { get; }
        public int MultiLineFinisherBonus { get; }
        public int HighComboClearBonus { get; }
        public float PlacementComboStepMultiplier { get; }
        public float PlacementComboMaxMultiplier { get; }
        public ScoreRoundingMode RoundingMode { get; }
        public ScoreCurvePoint[] LineMultiplierCurve { get; }
        public ScoreCurvePoint[] ComboMultiplierCurve { get; }

        public static ScoreConfig Default { get; } = CreateDefault();

        public ScoreConfig(
            int formulaVersion,
            int basePointsPerLine,
            int basePointsPerPlacement,
            int basePointsPerPlacedCell,
            int highRiskPlacementBonus,
            int multiLineFinisherBonus,
            int highComboClearBonus,
            float placementComboStepMultiplier,
            float placementComboMaxMultiplier,
            ScoreRoundingMode roundingMode,
            ScoreCurvePoint[] lineMultiplierCurve,
            ScoreCurvePoint[] comboMultiplierCurve)
        {
            if (formulaVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(formulaVersion), "Formula version must be positive.");
            if (basePointsPerLine < 0)
                throw new ArgumentOutOfRangeException(nameof(basePointsPerLine), "Base points cannot be negative.");
            if (basePointsPerPlacement < 0)
                throw new ArgumentOutOfRangeException(nameof(basePointsPerPlacement), "Placement base points cannot be negative.");
            if (basePointsPerPlacedCell < 0)
                throw new ArgumentOutOfRangeException(nameof(basePointsPerPlacedCell), "Placement per-cell points cannot be negative.");
            if (highRiskPlacementBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(highRiskPlacementBonus), "High risk bonus cannot be negative.");
            if (multiLineFinisherBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(multiLineFinisherBonus), "Finisher bonus cannot be negative.");
            if (highComboClearBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(highComboClearBonus), "High combo clear bonus cannot be negative.");
            if (placementComboStepMultiplier < 0f)
                throw new ArgumentOutOfRangeException(nameof(placementComboStepMultiplier), "Placement combo step multiplier cannot be negative.");
            if (placementComboMaxMultiplier < 1f)
                throw new ArgumentOutOfRangeException(nameof(placementComboMaxMultiplier), "Placement combo max multiplier must be at least 1.");
            if (lineMultiplierCurve == null || lineMultiplierCurve.Length == 0)
                throw new ArgumentException("Line multiplier curve must contain at least one point.", nameof(lineMultiplierCurve));
            if (comboMultiplierCurve == null || comboMultiplierCurve.Length == 0)
                throw new ArgumentException("Combo multiplier curve must contain at least one point.", nameof(comboMultiplierCurve));

            FormulaVersion = formulaVersion;
            BasePointsPerLine = basePointsPerLine;
            BasePointsPerPlacement = basePointsPerPlacement;
            BasePointsPerPlacedCell = basePointsPerPlacedCell;
            HighRiskPlacementBonus = highRiskPlacementBonus;
            MultiLineFinisherBonus = multiLineFinisherBonus;
            HighComboClearBonus = highComboClearBonus;
            PlacementComboStepMultiplier = placementComboStepMultiplier;
            PlacementComboMaxMultiplier = placementComboMaxMultiplier;
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

                basePointsPerLine:          150,
                basePointsPerPlacement:      25,
                basePointsPerPlacedCell:       8,
                highRiskPlacementBonus:       30,
                multiLineFinisherBonus:      250,
                highComboClearBonus:         150,
                placementComboStepMultiplier: 0.15f,
                placementComboMaxMultiplier:  2.5f,

                roundingMode: ScoreRoundingMode.Nearest,

                lineMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1, 1.6f),
                    new ScoreCurvePoint(2, 3.2f),
                    new ScoreCurvePoint(3, 5.8f),
                    new ScoreCurvePoint(4, 9.0f),
                    new ScoreCurvePoint(6, 11.0f),
                    new ScoreCurvePoint(8, 12.0f)
                },

                comboMultiplierCurve: new[]
                {
                    new ScoreCurvePoint(1,  1.3f),
                    new ScoreCurvePoint(2,  2.0f),
                    new ScoreCurvePoint(3,  2.8f),
                    new ScoreCurvePoint(5,  3.8f),
                    new ScoreCurvePoint(7,  5.0f),
                    new ScoreCurvePoint(10, 6.5f),
                    new ScoreCurvePoint(15, 8.5f),
                    new ScoreCurvePoint(20, 10.0f)
                });
        }
    }
}
