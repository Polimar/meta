using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BeatSaberAlefy.VR;

namespace BeatSaberAlefy.UI
{
    /// <summary>
    /// UI in-game: punteggio, barra vita, pannello game over / completato con Riprova e Torna al menu.
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        public Text ScoreText;
        public Text ComboText;
        public Image LifeBarFill;
        public GameObject PanelGameOver;
        public Text GameOverLabelText;
        public Text GameOverScoreText;
        public Text GameOverBestText;
        public Button RetryButton;
        public Button BackToMenuButton;
        public string GameSceneName = "Game";
        public string MenuSceneName = "Menu";
        public GameplayDirector Director;
        bool _highScoreChecked;
        float _gameStartTime = -1f;

        void Start()
        {
            ResolveUI();
            if (RetryButton != null)
                RetryButton.onClick.AddListener(OnRetry);
            if (BackToMenuButton != null)
                BackToMenuButton.onClick.AddListener(OnBackToMenu);
            if (PanelGameOver != null)
                PanelGameOver.SetActive(false);
            if (Director == null)
                Director = Object.FindFirstObjectByType<GameplayDirector>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.sortingOrder < 50)
                canvas.sortingOrder = 50;
            ResolveUI();
            EnsureLifeBarFillType();
        }

        void ResolveUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.transform : transform;

            if (LifeBarFill == null)
            {
                var fillGo = GameObject.Find("LifeBarFill");
                if (fillGo != null) LifeBarFill = fillGo.GetComponent<Image>();
                if (LifeBarFill == null)
                {
                    var bg = root.Find("LifeBarBg");
                    if (bg != null)
                    {
                        var fill = bg.Find("LifeBarFill");
                        if (fill != null) LifeBarFill = fill.GetComponent<Image>();
                    }
                    if (LifeBarFill == null)
                    {
                        var fill = root.Find("LifeBarFill");
                        if (fill != null) LifeBarFill = fill.GetComponent<Image>();
                    }
                }
            }

            if (PanelGameOver == null)
            {
                var t = root.Find("PanelGameOver");
                if (t != null) PanelGameOver = t.gameObject;
                else PanelGameOver = GameObject.Find("PanelGameOver");
            }
            if (PanelGameOver != null)
            {
                if (GameOverLabelText == null)
                {
                    var child = PanelGameOver.transform.Find("GameOverLabel");
                    if (child != null) GameOverLabelText = child.GetComponent<Text>();
                }
                if (GameOverScoreText == null)
                {
                    var child = PanelGameOver.transform.Find("GameOverScoreText");
                    if (child != null) GameOverScoreText = child.GetComponent<Text>();
                }
                if (GameOverBestText == null)
                {
                    var child = PanelGameOver.transform.Find("GameOverBestText");
                    if (child != null) GameOverBestText = child.GetComponent<Text>();
                }
                if (RetryButton == null)
                {
                    var child = PanelGameOver.transform.Find("RetryButton");
                    if (child != null) RetryButton = child.GetComponent<Button>();
                }
                if (BackToMenuButton == null)
                {
                    var child = PanelGameOver.transform.Find("BackToMenuButton");
                    if (child != null) BackToMenuButton = child.GetComponent<Button>();
                }
            }
            if (ScoreText == null)
            {
                var t = root.Find("ScoreText");
                if (t != null) ScoreText = t.GetComponent<Text>();
            }
        }

        void EnsureLifeBarFillType()
        {
            if (LifeBarFill == null) return;
            LifeBarFill.type = Image.Type.Filled;
            LifeBarFill.fillMethod = Image.FillMethod.Horizontal;
            LifeBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        void Update()
        {
            var state = GameState.Instance;
            if (state == null) return;

            if (LifeBarFill == null)
            {
                ResolveUI();
                EnsureLifeBarFillType();
            }
            if (LifeBarFill != null)
                LifeBarFill.fillAmount = Mathf.Clamp01((float)state.Life / GameState.MaxLife);

            if (ScoreText != null)
                ScoreText.text = "Score: " + state.Score;

            if (ComboText != null)
            {
                ComboText.gameObject.SetActive(state.ComboCount > 0);
                if (state.ComboCount > 0)
                    ComboText.text = "Combo x" + state.GetComboMultiplier() + " (" + state.ComboCount + ")";
            }

            var src = Director != null ? Director.AudioSource : null;
            var clip = src != null ? src.clip : null;
            if (src != null && clip != null && src.isPlaying && _gameStartTime < 0f)
                _gameStartTime = Time.time;

            float safeTime = (src != null && clip != null) ? src.time : 0f;
            float clipLen = clip != null ? clip.length : 0f;
            bool clipEnded = src != null && clip != null && !src.isPlaying;
            bool atEndByTime = clipLen > 0 && safeTime >= clipLen - 0.1f;
            bool atEndByDuration = clipLen > 1f && _gameStartTime >= 0f && (Time.time - _gameStartTime) >= clipLen - 0.5f;
            bool levelComplete = !state.IsGameOver && clipEnded && (atEndByTime || atEndByDuration);

            if (state.IsGameOver || levelComplete)
            {
                if (PanelGameOver != null && !PanelGameOver.activeSelf)
                {
                    PanelGameOver.SetActive(true);
                    if (GameOverLabelText != null)
                        GameOverLabelText.text = state.IsGameOver ? "Game Over" : "Completato";
                    if (GameOverScoreText != null)
                        GameOverScoreText.text = "Score: " + state.Score;
                    if (!_highScoreChecked)
                    {
                        _highScoreChecked = true;
                        var trackId = GameSessionData.SelectedTrackId;
                        var prevBest = HighScoreService.GetHighScore(trackId);
                        if (state.Score > prevBest)
                        {
                            HighScoreService.SetHighScore(trackId, state.Score);
                            if (GameOverBestText != null)
                                GameOverBestText.text = "New record!";
                        }
                        else if (GameOverBestText != null && prevBest > 0)
                            GameOverBestText.text = "Best: " + prevBest;
                    }
                }
            }
        }

        void OnBackToMenu()
        {
            SceneManager.LoadScene(MenuSceneName);
        }

        void OnRetry()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
