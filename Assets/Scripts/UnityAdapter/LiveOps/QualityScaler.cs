using UnityEngine;

namespace BlockPuzzle.UnityAdapter.LiveOps
{
    public class QualityScaler : MonoBehaviour
    {
        [Header("Thresholds")]
        [SerializeField] private int lowMemoryThresholdMB = 2048; // Below 2GB RAM
        
        private void Start()
        {
            EvaluateDeviceQuality();
        }

        private void EvaluateDeviceQuality()
        {
            int deviceMemory = SystemInfo.systemMemorySize;
            int processorCount = SystemInfo.processorCount;

            if (deviceMemory <= lowMemoryThresholdMB || processorCount <= 2)
            {
                ApplyLowEndOptimizations(deviceMemory);
            }
            else
            {
                ApplyHighEndSettings();
            }
        }

        private void ApplyLowEndOptimizations(int mem)
        {
            Debug.Log($"[Quality] Low-End Device Detected (RAM: {mem}MB). Optimizing...");
            
            // Limit frame rate to save battery and reduce thermal throttling
            Application.targetFrameRate = 30;
            
            // Downscale textures (if any heavy assets exist, unlikely in core puzzle but good for cosmetics)
            QualitySettings.globalTextureMipmapLimit = 1; 

            // Disable heavy post-processing / VSync
            QualitySettings.vSyncCount = 0; 
            QualitySettings.pixelLightCount = 0;
            
            // Note: If Unity's Addressables are used, we could load lower res asset bundles here
        }

        private void ApplyHighEndSettings()
        {
            Debug.Log("[Quality] High-End Device Detected. Maximum settings applied.");
            Application.targetFrameRate = 60;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.vSyncCount = 1; // Or 0 for unlocked 60 FPS
        }
    }
}