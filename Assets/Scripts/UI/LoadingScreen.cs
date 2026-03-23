using UnityEngine;
using UnityEngine.UIElements;

namespace UserInterface
{
    public class LoadingScreen : MonoBehaviour
    {
        private VisualElement _root;
        private Label _statusLabel;
        private VisualElement _progressFill;
        private bool _mapLoaded;
        private bool _gameReady;

        void Start()
        {
            _root         = GetComponent<UIDocument>().rootVisualElement.Q("Root");
            _statusLabel  = _root.Q<Label>("StatusLabel");
            _progressFill = _root.Q("ProgressFill");
            _root.style.display = DisplayStyle.None;

            GameManager.OnConnected      += OnConnected;
            GameManager.OnLoadProgress   += OnLoadProgress;
            GameManager.OnMapLoaded      += OnMapLoaded;
            GameManager.OnGameReady      += OnGameReady;
            GameManager.OnDisconnected   += OnDisconnected;
        }

        void OnDestroy()
        {
            GameManager.OnConnected      -= OnConnected;
            GameManager.OnLoadProgress   -= OnLoadProgress;
            GameManager.OnMapLoaded      -= OnMapLoaded;
            GameManager.OnGameReady      -= OnGameReady;
            GameManager.OnDisconnected   -= OnDisconnected;
        }

        private void OnConnected()
        {
            _mapLoaded = false;
            _gameReady = false;
            _root.style.opacity = 1f;
            _root.style.display = DisplayStyle.Flex;
            SetProgress(0f);
            _statusLabel.text = "Loading map…";
        }

        private void OnLoadProgress(float t)
        {
            // t is 0–1 from Simulation.Init; map to 10–90% of the bar
            SetProgress(0.1f + t * 0.8f);
        }

        private void OnMapLoaded()
        {
            _mapLoaded = true;
            SetProgress(0.9f);
            _statusLabel.text = "Joining game…";
            TryHide();
        }

        private void OnGameReady()
        {
            _gameReady = true;
            TryHide();
        }

        private void OnDisconnected()
        {
            _mapLoaded = false;
            _gameReady = false;
            _root.style.display = DisplayStyle.None;
        }

        private void TryHide()
        {
            if (!_mapLoaded || !_gameReady) return;
            SetProgress(1f);
            // Fade out, then hide after the transition completes.
            _root.schedule.Execute(() => _root.style.opacity = 0f).StartingIn(100);
            _root.schedule.Execute(() => _root.style.display = DisplayStyle.None).StartingIn(700);
        }

        private void SetProgress(float t)
        {
            _progressFill.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
        }
    }
}
