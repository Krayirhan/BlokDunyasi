using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Animation
{
    /// <summary>
    /// Blok animasyonlarının konfigürasyonları - Inspector'dan düzenlenebilir
    /// </summary>
    [System.Serializable]
    public class BlockAnimationPreset
    {
        [Header("=== PLACEMENT ===")]
        [SerializeField] public AnimationTiming placementTiming = new AnimationTiming(0.3f);
        [SerializeField] public float placementScaleMin = 0.95f;
        [SerializeField] public float placementScaleMax = 1.0f;
        [SerializeField] public bool placementEnableRotationSettle = true;

        [Header("=== LINE CLEAR ===")]
        [SerializeField] public AnimationTiming lineClearHighlight = new AnimationTiming(0.2f);
        [SerializeField] public AnimationTiming lineClearBurst = new AnimationTiming(0.2f);
        [SerializeField] public AnimationTiming lineClearVanish = new AnimationTiming(0.4f);
        [SerializeField] public float lineClearScaleBurst = 1.2f;
        [SerializeField] public float lineClearRotation = 45f;

        [Header("=== TRAY SPAWN ===")]
        [SerializeField] public AnimationTiming traySpawnTiming = new AnimationTiming(0.3f);
        [SerializeField] public bool traySpawnBounce = true;
        [SerializeField] public float traySpawnBounceHeight = 0.2f;

        [Header("=== DRAG STATE ===")]
        [SerializeField] public float dragScaleMultiplier = 1.1f;
        [SerializeField] public int dragSortingOrder = 15;
        [SerializeField] public AnimationTiming dragTransitionTiming = new AnimationTiming(0.1f);

        [Header("=== INVALID PLACEMENT ===")]
        [SerializeField] public int invalidPlacementShakeCount = 4;
        [SerializeField] public float invalidPlacementShakeIntensity = 5f;
        [SerializeField] public AnimationTiming invalidPlacementTiming = new AnimationTiming(0.3f);

        public static BlockAnimationPreset GetDefault()
        {
            return new BlockAnimationPreset();
        }
    }

    /// <summary>
    /// Tekrar kullanılabilir animasyon timing bilgisi
    /// </summary>
    [System.Serializable]
    public class AnimationTiming
    {
        [SerializeField] public float duration = 0.3f;

        public AnimationTiming() { }

        public AnimationTiming(float durationSeconds)
        {
            this.duration = durationSeconds;
        }
    }

    /// <summary>
    /// UI animasyon preseti
    /// </summary>
    [System.Serializable]
    public class UIAnimationPreset
    {
        [Header("=== SCORE ===")]
        [SerializeField] public AnimationTiming scoreBurstTiming = new AnimationTiming(0.4f);
        [SerializeField] public float scoreScaleMax = 1.15f;

        [Header("=== COMBO ===")]
        [SerializeField] public AnimationTiming comboBadgeTiming = new AnimationTiming(0.5f);
        [SerializeField] public int comboBadgeShakeCount = 6;
        [SerializeField] public float comboBadgeShakeIntensity = 2f;
        [SerializeField] public float comboBadgeScaleMax = 1.2f;

        [Header("=== PANEL TRANSITIONS ===")]
        [SerializeField] public AnimationTiming panelSlideTiming = new AnimationTiming(0.3f);
        [SerializeField] public float panelSlideDistance = 1920f;

        [Header("=== BUTTONS ===")]
        [SerializeField] public AnimationTiming buttonHoverTiming = new AnimationTiming(0.15f);
        [SerializeField] public float buttonHoverScale = 1.1f;

        [Header("=== FLOATING TEXT ===")]
        [SerializeField] public AnimationTiming floatingTextTiming = new AnimationTiming(1f);
        [SerializeField] public float floatingTextRiseDist = 100f;

        public static UIAnimationPreset GetDefault()
        {
            return new UIAnimationPreset();
        }
    }

    /// <summary>
    /// VFX animasyon preseti
    /// </summary>
    [System.Serializable]
    public class VFXAnimationPreset
    {
        [Header("=== CAMERA SHAKE ===")]
        [SerializeField] public float cameraShakeDuration = 0.1f;
        [SerializeField] public float cameraShakeIntensity = 1f;

        [Header("=== PARTICLES ===")]
        [SerializeField] public int lineClearParticleCount = 28;
        [SerializeField] public float lineClearParticleLifetime = 0.8f;
        [SerializeField] public float lineClearParticleSize = 0.1f;
        [SerializeField] public float lineClearParticleSpeedMin = 2f;
        [SerializeField] public float lineClearParticleSpeedMax = 5f;

        [Header("=== PLACEMENT FEEDBACK ===")]
        [SerializeField] public int placementDustCount = 5;
        [SerializeField] public float placementDustLifetime = 0.5f;
        [SerializeField] public float gridCellGlowDuration = 0.3f;

        public static VFXAnimationPreset GetDefault()
        {
            return new VFXAnimationPreset();
        }
    }
}
