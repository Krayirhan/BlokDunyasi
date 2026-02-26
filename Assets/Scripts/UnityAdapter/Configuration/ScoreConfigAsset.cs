using System.Collections.Generic;
using BlockPuzzle.Core.Rules;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Configuration
{
    /// <summary>
    /// ScriptableObject wrapper for score formula tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "Blok Dunyasi/Score Config")]
    public class ScoreConfigAsset : ScriptableObject
    {
        [Header("Formula")]
        [SerializeField] private int scoreFormulaVersion = ScoreConfig.DefaultFormulaVersion;
        [SerializeField] private int basePointsPerLine = 10;
        [SerializeField] private ScoreRoundingMode roundingMode = ScoreRoundingMode.Nearest;

        [Header("Line Multiplier Curve")]
        [SerializeField] private AnimationCurve lineMultiplierCurve = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(2f, 1.5f),
            new Keyframe(3f, 2.0f),
            new Keyframe(4f, 2.5f));
        [SerializeField] [Min(2)] private int lineCurveSampleMaxX = 10;

        [Header("Combo Multiplier Curve")]
        [SerializeField] private AnimationCurve comboMultiplierCurve = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(2f, 1.1f),
            new Keyframe(5f, 1.4f),
            new Keyframe(10f, 1.9f));
        [SerializeField] [Min(2)] private int comboCurveSampleMaxX = 20;

        [Header("Clamp")]
        [SerializeField] [Min(0f)] private float minMultiplier = 0f;
        [SerializeField] [Min(0f)] private float maxMultiplier = 10f;

        public ScoreConfig ToCoreConfig()
        {
            int formulaVersion = scoreFormulaVersion <= 0
                ? ScoreConfig.DefaultFormulaVersion
                : scoreFormulaVersion;
            int basePoints = basePointsPerLine < 0 ? 0 : basePointsPerLine;

            var linePoints = BuildCurvePoints(lineMultiplierCurve, lineCurveSampleMaxX);
            var comboPoints = BuildCurvePoints(comboMultiplierCurve, comboCurveSampleMaxX);

            return new ScoreConfig(
                formulaVersion: formulaVersion,
                basePointsPerLine: basePoints,
                roundingMode: roundingMode,
                lineMultiplierCurve: linePoints,
                comboMultiplierCurve: comboPoints);
        }

        private ScoreCurvePoint[] BuildCurvePoints(AnimationCurve curve, int maxX)
        {
            int clampedMaxX = Mathf.Max(2, maxX);
            var points = new List<ScoreCurvePoint>(clampedMaxX);

            var sourceCurve = curve != null && curve.keys != null && curve.keys.Length > 0
                ? curve
                : AnimationCurve.Linear(1f, 1f, clampedMaxX, 1f);

            for (int x = 1; x <= clampedMaxX; x++)
            {
                float sampled = sourceCurve.Evaluate(x);
                float clamped = Mathf.Clamp(sampled, minMultiplier, Mathf.Max(minMultiplier, maxMultiplier));
                points.Add(new ScoreCurvePoint(x, clamped));
            }

            return points.ToArray();
        }

        private void OnValidate()
        {
            if (scoreFormulaVersion <= 0)
                scoreFormulaVersion = ScoreConfig.DefaultFormulaVersion;

            if (basePointsPerLine < 0)
                basePointsPerLine = 0;

            if (lineCurveSampleMaxX < 2)
                lineCurveSampleMaxX = 2;

            if (comboCurveSampleMaxX < 2)
                comboCurveSampleMaxX = 2;

            if (maxMultiplier < minMultiplier)
                maxMultiplier = minMultiplier;
        }
    }
}
