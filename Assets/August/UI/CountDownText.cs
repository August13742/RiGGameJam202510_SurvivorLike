using TMPro;
using UnityEngine;

namespace Rhythm.UI
{
    public class CountDownText : MonoBehaviour
    {
        private TMP_Text _label;
        private float _timeLeft;
        private bool _isRunning;
        private bool _useUnscaled;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
            if (_label != null)
                _label.enabled = false;
        }

        private void Start()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Starts a countdown for the given duration.
        /// If useUnscaled is true, countdown ignores Time.timeScale.
        /// we typically want useUnscaled = false
        /// so that pausing the game pauses the countdown as well.
        /// </summary>
        public void StartCountdown(float duration, bool useUnscaled = false)
        {
            _timeLeft = Mathf.Max(0f, duration);
            _useUnscaled = useUnscaled;
            _isRunning = _timeLeft > 0f;

            if (_label != null)
                _label.enabled = _isRunning;
        }

        private void Update()
        {
            if (!_isRunning)
            {
                if (_label != null)
                    _label.enabled = false;
                return;
            }

            float dt = _useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            _timeLeft -= dt;

            if (_timeLeft > 0f)
            {
                if (_label != null)
                {
                    _label.text = _timeLeft.ToString("0.00");
                    _label.enabled = true;
                }
            }
            else
            {
                _isRunning = false;
                if (_label != null)
                    _label.enabled = false;

                Destroy(gameObject);
            }
        }
    }
}
