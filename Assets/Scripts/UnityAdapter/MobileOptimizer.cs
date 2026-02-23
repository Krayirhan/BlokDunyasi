// File: UnityAdapter/MobileOptimizer.cs

using UnityEngine;

namespace BlockPuzzle.UnityAdapter
{
    /// <summary>
    /// Mobile performance optimizations for Blok Dünyası
    /// </summary>
    public class MobileOptimizer : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool enableVSync = false;
        [SerializeField] private bool optimizeForMobile = true;
        
        [Header("Quality Settings")]
        [SerializeField] private bool reduceShadows = true;
        [SerializeField] private bool optimizeTextures = true;
        [SerializeField] private bool limitParticles = true;
        
        [Header("Input Optimizations")]
        [SerializeField] private bool optimizeTouch = true;
        [SerializeField] private int multiTouchLimit = 1;

        private void Awake()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Disable VSync for better mobile performance
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;
            
            if (optimizeForMobile && Application.isMobilePlatform)
            {
                ApplyMobileOptimizations();
            }
        }

        private void Start()
        {
            if (optimizeTouch && Application.isMobilePlatform)
            {
                // Limit multitouch to improve performance
                UnityEngine.Input.multiTouchEnabled = multiTouchLimit > 1;
            }
        }

        private void ApplyMobileOptimizations()
        {
            // Reduce shadow quality
            if (reduceShadows)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowResolution = ShadowResolution.Low;
            }
            
            // Optimize texture quality
            if (optimizeTextures)
            {
                QualitySettings.globalTextureMipmapLimit = 1; // Half resolution
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
            
            // Reduce particle limits
            if (limitParticles)
            {
                QualitySettings.particleRaycastBudget = 64;
            }
            
            // Optimize physics
            Physics.bounceThreshold = 2f;
            Physics.sleepThreshold = 0.05f;
            
            Debug.Log("[MobileOptimizer] Applied mobile optimizations");
        }

        /// <summary>
        /// Manually trigger garbage collection to reduce frame drops
        /// </summary>
        public void ForceGarbageCollection()
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Enable/disable mobile optimizations at runtime
        /// </summary>
        public void SetMobileOptimization(bool enable)
        {
            optimizeForMobile = enable;
            if (enable && Application.isMobilePlatform)
            {
                ApplyMobileOptimizations();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Force GC when app is paused to free memory
            if (pauseStatus)
            {
                ForceGarbageCollection();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Adjust frame rate based on focus
            if (hasFocus)
            {
                Application.targetFrameRate = targetFrameRate;
            }
            else
            {
                Application.targetFrameRate = 30; // Reduce when not focused
            }
        }
    }
}