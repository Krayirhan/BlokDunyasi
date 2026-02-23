// File: Core/Persistence/GameSettings.cs
using System;

namespace BlockPuzzle.Core.Persistence
{
    /// <summary>
    /// Player preferences and game settings.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        /// <summary>
        /// Master volume level (0.0 to 1.0).
        /// </summary>
        public float MasterVolume { get; set; } = 1.0f;
        
        /// <summary>
        /// Sound effects volume (0.0 to 1.0).
        /// </summary>
        public float SfxVolume { get; set; } = 1.0f;
        
        /// <summary>
        /// Background music volume (0.0 to 1.0).
        /// </summary>
        public float MusicVolume { get; set; } = 0.7f;
        
        /// <summary>
        /// Whether sound effects are enabled.
        /// </summary>
        public bool SfxEnabled { get; set; } = true;
        
        /// <summary>
        /// Whether background music is enabled.
        /// </summary>
        public bool MusicEnabled { get; set; } = true;
        
        /// <summary>
        /// Whether vibration/haptic feedback is enabled.
        /// </summary>
        public bool VibrationEnabled { get; set; } = true;
        
        /// <summary>
        /// Whether to show placement hints/guides.
        /// </summary>
        public bool ShowPlacementHints { get; set; } = true;
        
        /// <summary>
        /// Whether to show valid placement indicators.
        /// </summary>
        public bool ShowValidPlacements { get; set; } = false;
        
        /// <summary>
        /// Whether to use automatic dark mode based on system.
        /// </summary>
        public bool AutoDarkMode { get; set; } = true;
        
        /// <summary>
        /// Manual dark mode setting (when AutoDarkMode is false).
        /// </summary>
        public bool DarkMode { get; set; } = false;
        
        /// <summary>
        /// Animation speed multiplier (0.5 to 2.0).
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;
        
        /// <summary>
        /// Whether to enable particle effects.
        /// </summary>
        public bool ParticleEffects { get; set; } = true;
        
        /// <summary>
        /// Screen shake intensity (0.0 to 1.0).
        /// </summary>
        public float ScreenShakeIntensity { get; set; } = 0.5f;
        
        /// <summary>
        /// Whether to show FPS counter (for debug builds).
        /// </summary>
        public bool ShowFpsCounter { get; set; } = false;
        
        /// <summary>
        /// Language/locale setting.
        /// </summary>
        public string Language { get; set; } = "en";
        
        /// <summary>
        /// Preferred board theme.
        /// </summary>
        public string BoardTheme { get; set; } = "classic";
        
        /// <summary>
        /// Preferred block theme.
        /// </summary>
        public string BlockTheme { get; set; } = "classic";
        
        /// <summary>
        /// Whether tutorial has been completed.
        /// </summary>
        public bool TutorialCompleted { get; set; } = false;
        
        /// <summary>
        /// Last played version (for showing update notes).
        /// </summary>
        public string LastPlayedVersion { get; set; } = "1.0.0";
        
        /// <summary>
        /// Default settings instance.
        /// </summary>
        public static GameSettings Default => new GameSettings();
        
        /// <summary>
        /// Creates default game settings.
        /// </summary>
        /// <returns>Default settings instance</returns>
        public static GameSettings CreateDefault()
        {
            return new GameSettings();
        }
        
        /// <summary>
        /// Validates and clamps settings values to valid ranges.
        /// </summary>
        public void Validate()
        {
            MasterVolume = ClampVolume(MasterVolume);
            SfxVolume = ClampVolume(SfxVolume);
            MusicVolume = ClampVolume(MusicVolume);
            AnimationSpeed = ClampFloat(AnimationSpeed, 0.5f, 2.0f);
            ScreenShakeIntensity = ClampFloat(ScreenShakeIntensity, 0.0f, 1.0f);
            
            // Ensure non-null strings
            Language ??= "en";
            BoardTheme ??= "classic";
            BlockTheme ??= "classic";
            LastPlayedVersion ??= "1.0.0";
        }
        
        private static float ClampVolume(float volume)
        {
            return ClampFloat(volume, 0.0f, 1.0f);
        }
        
        private static float ClampFloat(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}