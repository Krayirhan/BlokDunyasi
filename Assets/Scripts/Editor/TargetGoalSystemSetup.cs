// File: Editor/TargetGoalSystemSetup.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using BlockPuzzle.UnityAdapter.UI;

namespace BlockPuzzle.UnityAdapter.Editor
{
    public class TargetGoalSystemSetup : MonoBehaviour
    {
        [MenuItem("Tools/Setup Target Goal System")]
        public static void SetupTargetGoalSystem()
        {
            // Find HudView in scene
            HudView hudView = Object.FindFirstObjectByType<HudView>();
            if (hudView == null)
            {
                EditorUtility.DisplayDialog("Error", "HudView not found in scene!", "OK");
                return;
            }

            Canvas hudCanvas = hudView.GetComponentInParent<Canvas>();
            if (hudCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "Canvas parent not found for HudView!", "OK");
                return;
            }

            Transform hudParent = hudView.transform;

            // Create TargetGoalContainer if it doesn't exist
            Transform targetGoalContainer = hudParent.Find("TargetGoalContainer");
            if (targetGoalContainer == null)
            {
                GameObject containerObj = new GameObject("TargetGoalContainer");
                containerObj.transform.SetParent(hudParent, false);
                targetGoalContainer = containerObj.transform;

                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0.5f, 1f);
                containerRect.anchorMax = new Vector2(0.5f, 1f);
                containerRect.pivot = new Vector2(0.5f, 1f);
                containerRect.anchoredPosition = new Vector2(0f, -180f);
                containerRect.sizeDelta = new Vector2(400f, 120f);

                Undo.RegisterCreatedObjectUndo(containerObj, "Create TargetGoalContainer");
            }

            // Create Background Progress Bar (Gradient)
            Transform bgBarTransform = targetGoalContainer.Find("ProgressBarBackground");
            if (bgBarTransform == null)
            {
                GameObject bgBarObj = new GameObject("ProgressBarBackground");
                bgBarObj.transform.SetParent(targetGoalContainer, false);
                RectTransform bgBarRect = bgBarObj.AddComponent<RectTransform>();
                bgBarRect.anchorMin = Vector2.zero;
                bgBarRect.anchorMax = new Vector2(1f, 0f);
                bgBarRect.pivot = new Vector2(0.5f, 0f);
                bgBarRect.offsetMin = new Vector2(20f, 5f);
                bgBarRect.offsetMax = new Vector2(-20f, 35f);

                Image bgImg = bgBarObj.AddComponent<Image>();
                bgImg.type = Image.Type.Simple;
                bgImg.color = new Color(0.1f, 0.15f, 0.4f, 1f); // Dark blue base

                Undo.RegisterCreatedObjectUndo(bgBarObj, "Create ProgressBarBackground");
            }

            // Create ProgressBar (Yellow fill)
            Transform progressBarTransform = targetGoalContainer.Find("ProgressBar");
            Image progressBar;
            if (progressBarTransform == null)
            {
                GameObject pbarObj = new GameObject("ProgressBar");
                pbarObj.transform.SetParent(targetGoalContainer, false);
                RectTransform pbarRect = pbarObj.AddComponent<RectTransform>();
                pbarRect.anchorMin = Vector2.zero;
                pbarRect.anchorMax = new Vector2(1f, 0f);
                pbarRect.pivot = new Vector2(0.5f, 0f);
                pbarRect.offsetMin = new Vector2(20f, 5f);
                pbarRect.offsetMax = new Vector2(-20f, 35f);

                Image img = pbarObj.AddComponent<Image>();
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.color = new Color(1f, 1f, 0f, 1f); // Yellow fill
                progressBar = img;

                Undo.RegisterCreatedObjectUndo(pbarObj, "Create ProgressBar");
            }
            else
            {
                progressBar = progressBarTransform.GetComponent<Image>();
            }

            // Create TargetText
            Transform targetTextTransform = targetGoalContainer.Find("TargetText");
            TextMeshProUGUI targetText;
            if (targetTextTransform == null)
            {
                GameObject ttxtObj = new GameObject("TargetText");
                ttxtObj.transform.SetParent(targetGoalContainer, false);
                RectTransform ttxtRect = ttxtObj.AddComponent<RectTransform>();
                ttxtRect.anchorMin = new Vector2(0f, 1f);
                ttxtRect.anchorMax = new Vector2(0f, 1f);
                ttxtRect.pivot = new Vector2(0f, 1f);
                ttxtRect.anchoredPosition = new Vector2(10f, 0f);
                ttxtRect.sizeDelta = new Vector2(200f, 50f);

                CanvasRenderer cr1 = ttxtObj.AddComponent<CanvasRenderer>();
                targetText = ttxtObj.AddComponent<TextMeshProUGUI>();
                targetText.text = "Hedef: 500";
                targetText.fontSize = 36;
                targetText.color = new Color(1f, 0.95f, 0.72f, 1f);
                targetText.alignment = TextAlignmentOptions.TopLeft;
                targetText.fontStyle = FontStyles.Bold;

                Undo.RegisterCreatedObjectUndo(ttxtObj, "Create TargetText");
            }
            else
            {
                targetText = targetTextTransform.GetComponent<TextMeshProUGUI>();
            }

            // Add TargetGoalSystem component
            TargetGoalSystem targetGoalSystem = targetGoalContainer.GetComponent<TargetGoalSystem>();
            if (targetGoalSystem == null)
            {
                targetGoalSystem = targetGoalContainer.gameObject.AddComponent<TargetGoalSystem>();
                Undo.RegisterCreatedObjectUndo(targetGoalContainer.gameObject, "Add TargetGoalSystem");
            }

            // Wire up references in TargetGoalSystem
            SerializedObject so = new SerializedObject(targetGoalSystem);
            so.FindProperty("progressBar").objectReferenceValue = progressBar;
            so.FindProperty("targetText").objectReferenceValue = targetText;
            so.FindProperty("initialGoal").intValue = 500;
            so.FindProperty("goalIncrement").intValue = 500;
            so.FindProperty("showProgressText").boolValue = true;
            so.ApplyModifiedProperties();

            // Wire up reference in HudView
            SerializedObject hudViewSO = new SerializedObject(hudView);
            hudViewSO.FindProperty("targetGoalSystem").objectReferenceValue = targetGoalSystem;
            hudViewSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(hudView);
            EditorUtility.SetDirty(targetGoalSystem);

            EditorUtility.DisplayDialog(
                "Success",
                "Target Goal System setup complete!\n\nYou can now configure the values in the Inspector.",
                "OK");
        }
    }
}
#endif
