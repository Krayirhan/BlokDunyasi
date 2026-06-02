using System;
using UnityEngine;
using UnityEditor;
using BlockPuzzle.Core.LiveOps;
using BlockPuzzle.Core.Monetization;
using BlockPuzzle.Core.Social;
using BlockPuzzle.Core.Meta;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.Grid;
using BlockPuzzle.UnityAdapter.LiveOps;
using BlockPuzzle.Core.RNG;
using BlockPuzzle.UnityAdapter;

namespace BlockPuzzle.EditorTools
{
    public class AutoSetupGame : EditorWindow
    {
        [MenuItem("BlokDunyasi/1-Tıkla Oyunu Kur (Game Setup)")]
        public static void SetupEverything()
        {
            Debug.Log("🏗️ BlokDunyasi Otomatik Kurulum Başlıyor...");

            // 1. Scene Managers Kurulumu
            SetupGameManager();
            SetupMonetizationManager();
            SetupSocialManager();
            SetupLiveOpsManager();
            
            // 2. Örnek ScriptableObject Configleri Yaratma
            CreateConfig<SeasonalEventConfig>("SeasonalEventConfig");
            CreateConfig<ProductDefinition>("StarterPack_Product");
            CreateConfig<BlockPuzzle.Core.Meta.Missions.MissionDefinition>("Mission_FirstWin");
            CreateConfig<BlockPuzzle.Core.Meta.Cosmetics.CosmeticTheme>("DefaultTheme");

            Debug.Log("✅ Tüm Sahneler ve Referanslar Başarıyla Oluşturuldu! (Manager'lara sahneden erişebilirsiniz.)");
        }

        private static void SetupGameManager()
        {
            if (GameObject.Find("GameManager") != null) return;
            
            GameObject go = new GameObject("GameManager");
            AddComponentByTypeName(go, "BlockPuzzle.UnityAdapter.Boot.GameBootstrap", "BlockPuzzleUnityAdapter");
            // vb. temel scriptler
        }

        private static void SetupMonetizationManager()
        {
            if (GameObject.Find("MonetizationManager") != null) return;

            GameObject go = new GameObject("MonetizationManager");
            AddComponentByTypeName(go, "BlockPuzzle.Core.Monetization.StoreManager", "BlockPuzzleCore");
            AddComponentByTypeName(go, "BlockPuzzle.Core.Monetization.EntitlementManager", "BlockPuzzleCore");
            AddComponentByTypeName(go, "BlockPuzzle.Core.Monetization.ContinueEconomyManager", "BlockPuzzleCore");
        }

        private static void SetupSocialManager()
        {
            if (GameObject.Find("SocialManager") != null) return;

            GameObject go = new GameObject("SocialManager");
            AddComponentByTypeName(go, "BlockPuzzle.UnityAdapter.Social.LeaderboardManager", "BlockPuzzleUnityAdapter");
            AddComponentByTypeName(go, "BlockPuzzle.Core.Social.DailyChallengeManager", "BlockPuzzleCore");
        }

        private static void SetupLiveOpsManager()
        {
            if (GameObject.Find("LiveOpsManager") != null) return;

            GameObject go = new GameObject("LiveOpsManager");
            AddComponentByTypeName(go, "BlockPuzzle.Core.LiveOps.RemoteConfigManager", "BlockPuzzleCore");
            AddComponentByTypeName(go, "BlockPuzzle.UnityAdapter.LiveOps.QualityScaler", "BlockPuzzleUnityAdapter");
        }

        private static void AddComponentByTypeName(GameObject go, string typeFullName, string assemblyName)
        {
            try
            {
                var type = Type.GetType(typeFullName + ", " + assemblyName);
                if (type == null)
                {
                    Debug.LogWarning($"AutoSetup: Type not found: {typeFullName} in {assemblyName}");
                    return;
                }

                if (!typeof(Component).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"AutoSetup: Type {typeFullName} is not a Component.");
                    return;
                }

                go.AddComponent(type);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AutoSetup AddComponent failed: {ex.Message}");
            }
        }

        private static void CreateConfig<T>(string configName) where T : ScriptableObject
        {
            string folderPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string path = $"{folderPath}/{configName}.asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            {
                T asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"📁 Config Oluşturuldu: {path}");
            }
        }
    }
}
