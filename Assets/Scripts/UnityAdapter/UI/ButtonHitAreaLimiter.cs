using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI
{
    public class ButtonHitAreaLimiter : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private RectTransform customHitZone;

        public void SetTarget(Graphic graphic, RectTransform hitZone = null)
        {
            targetGraphic = graphic;
            customHitZone = hitZone;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (targetGraphic == null)
                return true;

            RectTransform limitRect = customHitZone != null
                ? customHitZone
                : targetGraphic.transform as RectTransform;

            if (limitRect == null)
                return true;

            if (!RectTransformUtility.RectangleContainsScreenPoint(limitRect, sp, eventCamera))
                return false;

            var raycastFilter = targetGraphic as ICanvasRaycastFilter;
            if (raycastFilter != null && !ReferenceEquals(raycastFilter, this))
                return raycastFilter.IsRaycastLocationValid(sp, eventCamera);

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetGraphic == null)
            {
                var button = GetComponent<Button>();
                if (button != null && button.targetGraphic != null)
                    targetGraphic = button.targetGraphic;
            }
        }
#endif
    }
}
