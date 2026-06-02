using UnityEngine;
using UnityEditor;
using TMPro;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.UnityAdapter.Editor
{
    public class PrefabCreator
    {
        [MenuItem("BlokDunyasi/Setup/Create Animation Prefabs")]
        public static void CreateAnimationPrefabs()
        {
            string vfxPath = "Assets/Prefabs/VFX/";
            string uiPath = "Assets/Prefabs/UI/";

            // 1. LineParticle Prefab
            CreateLineParticlePrefab(vfxPath);

            // 2. DustParticle Prefab
            CreateDustParticlePrefab(vfxPath);

            // 3. FloatingText Prefab
            CreateFloatingTextPrefab(uiPath);

            Debug.Log("[PrefabCreator] ✅ 3 prefab başarıyla oluşturuldu!");
            AssetDatabase.Refresh();
        }

        private static void CreateLineParticlePrefab(string path)
        {
            GameObject prefab = new GameObject("LineParticle");
            
            // SpriteRenderer
            var sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(1f, 1f, 1f, 1f);
            sr.sortingOrder = 10;

            // Rigidbody2D
            var rb = prefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // CanvasGroup
            var cg = prefab.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            // Prefab olarak kaydet
            PrefabUtility.SaveAsPrefabAsset(prefab, path + "LineParticle.prefab");
            Object.DestroyImmediate(prefab);
            Debug.Log("✅ LineParticle.prefab oluşturuldu");
        }

        private static void CreateDustParticlePrefab(string path)
        {
            GameObject prefab = new GameObject("DustParticle");
            
            // SpriteRenderer
            var sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            sr.sortingOrder = 5;

            // Rigidbody2D
            var rb = prefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // CanvasGroup
            var cg = prefab.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            // Prefab olarak kaydet
            PrefabUtility.SaveAsPrefabAsset(prefab, path + "DustParticle.prefab");
            Object.DestroyImmediate(prefab);
            Debug.Log("✅ DustParticle.prefab oluşturuldu");
        }

        private static void CreateFloatingTextPrefab(string path)
        {
            // Canvas içinde olmadığı için, world space text oluştur
            GameObject prefab = new GameObject("FloatingText");

            // TextMeshPro
            var textMesh = prefab.AddComponent<TextMeshPro>();
            textMesh.text = "+50";
            textMesh.fontSize = 40;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = new Color(1f, 0.9f, 0.3f, 1f);

            // RectTransform
            var rectTransform = prefab.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);

            // CanvasGroup
            var cg = prefab.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            // Prefab olarak kaydet
            PrefabUtility.SaveAsPrefabAsset(prefab, path + "FloatingText.prefab");
            Object.DestroyImmediate(prefab);
            Debug.Log("✅ FloatingText.prefab oluşturuldu");
        }
    }
}
