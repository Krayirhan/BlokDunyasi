using UnityEngine;

namespace BlockPuzzle.UnityAdapter.UI
{
    [DisallowMultipleComponent]
    public class PlayButtonPulseEffect : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRect;
        [SerializeField, Min(0.05f)] private float pulseSpeed = 1.45f;
        [SerializeField, Range(0f, 0.08f)] private float pulseAmount = 0.024f;
        [SerializeField] private bool useUnscaledTime = true;

        private RectTransform _buttonRect;
        private Vector3 _baseScale;
        private bool _hasBaseScale;

        private void Reset()
        {
            targetRect = transform as RectTransform;
        }

        private void Awake()
        {
            Initialize();
            ApplyFrame(0.6f);
        }

        private void OnEnable()
        {
            Initialize();
            ApplyFrame(0.6f);
        }

        private void Update()
        {
            if (!Initialize())
                return;

            float t = EvaluateWave();
            ApplyFrame(t);
        }

        private void OnDisable()
        {
            RestoreBaseScale();
        }

        private void OnDestroy()
        {
            RestoreBaseScale();
        }

        private void OnValidate()
        {
            pulseSpeed = Mathf.Max(0.05f, pulseSpeed);
            pulseAmount = Mathf.Clamp(pulseAmount, 0f, 0.08f);
        }

        private bool Initialize()
        {
            if (_buttonRect == null)
                _buttonRect = targetRect != null ? targetRect : transform as RectTransform;

            if (_buttonRect == null)
                return false;

            if (!_hasBaseScale)
            {
                _baseScale = _buttonRect.localScale;
                _hasBaseScale = true;
            }

            return true;
        }

        private float EvaluateWave()
        {
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float wave = Mathf.PingPong(time * pulseSpeed, 1f);
            return Mathf.SmoothStep(0f, 1f, wave);
        }

        private void ApplyFrame(float t)
        {
            if (_buttonRect == null || !_hasBaseScale)
                return;

            _buttonRect.localScale = _baseScale * (1f + (pulseAmount * t));
        }

        private void RestoreBaseScale()
        {
            if (_buttonRect != null && _hasBaseScale)
                _buttonRect.localScale = _baseScale;
        }
    }
}
