using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeatSaberAlefy.Alefy;

namespace BeatSaberAlefy.UI
{
    /// <summary>
    /// Lobby: lista tracce da Alefy con stato (Pronto / In coda / Download… / Analisi…),
    /// selezione singola, pulsante Start abilitato solo se traccia selezionata è pronta.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("UI")]
        public Button ButtonLoadFromAlefy;
        public Button ButtonSearch;
        public InputField SearchInputField;
        public Button ButtonStartGame;
        public Text StatusText;
        [Tooltip("Container per i pulsanti delle tracce Alefy")]
        public Transform AlefyTrackListContainer;
        [Tooltip("Prefab pulsante per una traccia (Button + Text per titolo; opzionale secondo Text per stato)")]
        public GameObject AlefyTrackButtonPrefab;
        [Tooltip("Colore per traccia selezionata")]
        public Color SelectedColor = new Color(0.3f, 0.6f, 1f);
        [Tooltip("Colore per traccia non selezionata")]
        public Color NormalColor = Color.white;

        [Header("Scene")]
        public string GameSceneName = "Game";

        IAlefyClient _alefyClient;
        AlefySongMetadata[] _alefyTracks;
        string _selectedTrackId;
        int _selectedIndex = -1;
        float _refreshTimer;
        const float RefreshInterval = 0.4f;
        readonly List<GameObject> _trackRows = new List<GameObject>();
        int _thumbnailLoadsInFlight;
        const int MaxConcurrentThumbnails = 4;
        int _currentPage;
        const int TracksPerPage = 6;
        Button _paginationPrev;
        Button _paginationNext;

        void Start()
        {
            ResolveTrackListContainer();
            _alefyClient = new AlefyService();
            var settings = AlefySettings.Load();
            if (settings != null && _alefyClient is AlefyService svc)
            {
                svc.BaseUrl = settings.BaseUrl;
                svc.AuthToken = settings.AuthToken;
            }

            var queue = TrackPreparationQueue.Instance;
            queue.AlefyClient = _alefyClient;

            if (ButtonLoadFromAlefy != null)
                ButtonLoadFromAlefy.onClick.AddListener(() => OnLoadFromAlefy(null));
            if (ButtonSearch != null)
                ButtonSearch.onClick.AddListener(OnSearch);
            if (SearchInputField != null)
                SearchInputField.onEndEdit.AddListener(s => { if (!string.IsNullOrEmpty(s)) OnSearch(); });
            if (ButtonStartGame != null)
                ButtonStartGame.onClick.AddListener(OnStartGame);

            SetStatus("Carica lista tracce da Alefy o cerca");
            UpdateStartButton();
        }

        void Update()
        {
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                RefreshTrackListState();
                UpdateStartButton();
            }
        }

        void OnLoadFromAlefy(string searchQuery)
        {
            StartCoroutine(LoadFromAlefyCoroutine(searchQuery));
        }

        void OnSearch()
        {
            var q = SearchInputField != null ? SearchInputField.text : null;
            OnLoadFromAlefy(string.IsNullOrWhiteSpace(q) ? null : q.Trim());
        }

        IEnumerator LoadFromAlefyCoroutine(string searchQuery)
        {
            SetStatus(string.IsNullOrEmpty(searchQuery) ? "Caricamento tracce da Alefy..." : "Ricerca in corso...");
            if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = false;
            if (ButtonSearch != null) ButtonSearch.interactable = false;

            var svc = _alefyClient as AlefyService;
            if (svc == null)
            {
                SetStatus("Client Alefy non disponibile.");
                if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = true;
                if (ButtonSearch != null) ButtonSearch.interactable = true;
                yield break;
            }

            AlefySongMetadata[] tracks = null;
            yield return svc.GetSongsAsyncCoroutine(searchQuery, t => tracks = t);
            _alefyTracks = tracks;

            if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = true;
            if (ButtonSearch != null) ButtonSearch.interactable = true;

            if (_alefyTracks == null || _alefyTracks.Length == 0)
            {
                SetStatus(string.IsNullOrEmpty(searchQuery) ? "Nessuna traccia da Alefy. Controlla BaseUrl e AuthToken in AlefySettings." : "Nessun risultato.");
                yield break;
            }

            _currentPage = 0;
            BuildTrackList();
            SetStatus($"Caricate {_alefyTracks.Length} tracce. Scegli una e attendi che sia Pronto, poi Avvia.");
        }

        static string FormatDuration(float? seconds)
        {
            if (seconds == null || seconds.Value <= 0) return "—";
            int s = (int)seconds.Value;
            return (s / 60) + ":" + (s % 60).ToString("D2");
        }

        int MaxPage => _alefyTracks == null || _alefyTracks.Length == 0 ? 0 : Mathf.Max(0, (_alefyTracks.Length - 1) / TracksPerPage);

        void BuildTrackList()
        {
            _trackRows.Clear();
            if (AlefyTrackListContainer == null || AlefyTrackButtonPrefab == null) return;

            foreach (Transform c in AlefyTrackListContainer)
                Destroy(c.gameObject);

            for (int row = 0; row < TracksPerPage; row++)
            {
                var go = Instantiate(AlefyTrackButtonPrefab, AlefyTrackListContainer);
                _trackRows.Add(go);
                EnsureRowLayout(go);
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
                le.minHeight = 56f;
                le.preferredHeight = 56f;
            }

            var paginationRow = new GameObject("PaginationRow");
            paginationRow.transform.SetParent(AlefyTrackListContainer, false);
            var pagRect = paginationRow.AddComponent<RectTransform>();
            var pagLe = paginationRow.AddComponent<LayoutElement>();
            pagLe.minHeight = 44f;
            pagLe.preferredHeight = 44f;
            var pagHlg = paginationRow.AddComponent<HorizontalLayoutGroup>();
            pagHlg.spacing = 16f;
            pagHlg.childAlignment = TextAnchor.MiddleCenter;
            pagHlg.childForceExpandWidth = false;
            pagHlg.childForceExpandHeight = false;

            _paginationPrev = CreatePaginationButton("← Prev", paginationRow.transform);
            _paginationNext = CreatePaginationButton("Next →", paginationRow.transform);
            if (_paginationPrev != null) _paginationPrev.onClick.AddListener(OnPrevPage);
            if (_paginationNext != null) _paginationNext.onClick.AddListener(OnNextPage);

            RefillPageRows();
            UpdatePaginationButtons();
        }

        Button CreatePaginationButton(string label, Transform parent)
        {
            var go = new GameObject("PaginationButton");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 100f;
            le.preferredHeight = 36f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 0.95f);
            var btn = go.AddComponent<Button>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return btn;
        }

        void OnPrevPage()
        {
            if (_currentPage <= 0) return;
            _currentPage--;
            RefillPageRows();
            UpdatePaginationButtons();
        }

        void OnNextPage()
        {
            if (_currentPage >= MaxPage) return;
            _currentPage++;
            RefillPageRows();
            UpdatePaginationButtons();
        }

        void UpdatePaginationButtons()
        {
            if (_paginationPrev != null) _paginationPrev.interactable = _currentPage > 0;
            if (_paginationNext != null) _paginationNext.interactable = _currentPage < MaxPage;
        }

        void RefillPageRows()
        {
            if (_alefyTracks == null || _trackRows.Count != TracksPerPage) return;
            var queue = TrackPreparationQueue.Instance;
            for (int row = 0; row < TracksPerPage; row++)
            {
                int globalIndex = _currentPage * TracksPerPage + row;
                var go = _trackRows[row];
                go.SetActive(true);
                Text titleLabel = null;
                Text durationLabel = null;
                Image thumbImg = null;
                foreach (var t in go.GetComponentsInChildren<Text>(true))
                {
                    if (t.gameObject.name == "DurationText")
                        durationLabel = t;
                    else if (titleLabel == null)
                        titleLabel = t;
                }
                var thumbTr = go.transform.Find("Thumbnail");
                if (thumbTr != null) thumbImg = thumbTr.GetComponent<Image>();
                if (thumbImg != null) { thumbImg.sprite = null; thumbImg.color = new Color(0.2f, 0.2f, 0.2f, 1f); }

                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();

                if (globalIndex < _alefyTracks.Length)
                {
                    var meta = _alefyTracks[globalIndex];
                    int index = globalIndex;
                    string stateLine = queue != null ? queue.GetStateLabel(meta.Id) : "";
                    if (titleLabel != null)
                        titleLabel.text = meta.Title + " - " + meta.Artist + "\n" + stateLine;
                    if (durationLabel != null)
                        durationLabel.text = FormatDuration(meta.DurationSeconds);
                    if (!string.IsNullOrEmpty(meta.CoverArtUrl) && thumbImg != null)
                        StartCoroutine(LoadTrackThumbnailWithRetry(meta.CoverArtUrl, thumbImg));
                    if (btn != null)
                        btn.onClick.AddListener(() => OnTrackClicked(index));
                    if (btn != null)
                    {
                        var colors = btn.colors;
                        colors.normalColor = _selectedIndex == index ? SelectedColor : NormalColor;
                        btn.colors = colors;
                    }
                }
                else
                {
                    go.SetActive(false);
                }
            }
            UpdatePaginationButtons();
        }

        void EnsureRowLayout(GameObject row)
        {
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) return;
            hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var rect = row.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(rect.sizeDelta.x, 56f);

            var texts = new List<Text>(row.GetComponentsInChildren<Text>(true));
            if (texts.Count == 0) return;
            var mainText = texts[0];
            var mainRect = mainText.GetComponent<RectTransform>();
            if (mainRect != null)
            {
                var flex = mainText.gameObject.GetComponent<LayoutElement>();
                if (flex == null) flex = mainText.gameObject.AddComponent<LayoutElement>();
                flex.flexibleWidth = 1f;
                flex.minWidth = 80f;
            }

            if (row.transform.Find("Thumbnail") == null)
            {
                var thumbGo = new GameObject("Thumbnail");
                thumbGo.transform.SetParent(row.transform, false);
                thumbGo.transform.SetAsFirstSibling();
                var thumbRect = thumbGo.AddComponent<RectTransform>();
                thumbRect.anchorMin = new Vector2(0, 0.5f);
                thumbRect.anchorMax = new Vector2(0, 0.5f);
                thumbRect.pivot = new Vector2(0, 0.5f);
                thumbRect.anchoredPosition = Vector2.zero;
                thumbRect.sizeDelta = new Vector2(48, 48);
                var thumbLe = thumbGo.AddComponent<LayoutElement>();
                thumbLe.preferredWidth = 48f;
                thumbLe.preferredHeight = 48f;
                var img = thumbGo.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            if (row.transform.Find("DurationText") == null)
            {
                var durGo = new GameObject("DurationText");
                durGo.transform.SetParent(row.transform, false);
                var durRect = durGo.AddComponent<RectTransform>();
                var durLe = durGo.AddComponent<LayoutElement>();
                durLe.preferredWidth = 52f;
                durLe.minWidth = 52f;
                var durText = durGo.AddComponent<Text>();
                durText.text = "—";
                durText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                durText.fontSize = 14;
                durText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                durText.alignment = TextAnchor.MiddleCenter;
            }
        }

        IEnumerator LoadTrackThumbnailWithRetry(string url, Image target)
        {
            yield return LoadTrackThumbnailSafe(url, target);
            if (target != null && target.gameObject != null && target.sprite == null && !string.IsNullOrEmpty(url))
                yield return LoadTrackThumbnailSafe(url, target);
        }

        IEnumerator LoadTrackThumbnailSafe(string url, Image target)
        {
            if (target == null || string.IsNullOrEmpty(url)) yield break;
            while (_thumbnailLoadsInFlight >= MaxConcurrentThumbnails)
                yield return null;
            _thumbnailLoadsInFlight++;
            try
            {
                yield return LoadTrackThumbnail(url, target);
            }
            finally
            {
                _thumbnailLoadsInFlight--;
            }
        }

        IEnumerator LoadTrackThumbnail(string url, Image target)
        {
            if (target == null || target.gameObject == null || string.IsNullOrEmpty(url)) yield break;
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                if (AlefySettings.Load() is AlefySettings settings && !string.IsNullOrEmpty(settings.AuthToken))
                    req.SetRequestHeader("Authorization", "Bearer " + settings.AuthToken);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) yield break;
                if (target == null || target.gameObject == null) yield break;
                var tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null || tex.width <= 0 || tex.height <= 0) yield break;
                if (target == null || target.gameObject == null) yield break;
                try
                {
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    if (target != null && target.gameObject != null)
                    {
                        target.sprite = sprite;
                        target.color = Color.white;
                        target.preserveAspect = true;
                    }
                }
                catch (System.Exception)
                {
                    if (tex != null) Destroy(tex);
                }
            }
        }

        void OnTrackClicked(int index)
        {
            if (_alefyTracks == null || index < 0 || index >= _alefyTracks.Length) return;
            var meta = _alefyTracks[index];
            if (meta == null) return;

            _selectedIndex = index;
            _selectedTrackId = meta.Id;

            RefreshTrackListState();
            UpdateStartButton();
            var queue = TrackPreparationQueue.Instance;
            SetStatus($"Selezionata: {meta.Title}. Stato: {(queue != null ? queue.GetStateLabel(_selectedTrackId) : "")}");
            if (queue != null && !TrackCache.Instance.IsReady(_selectedTrackId))
                StartCoroutine(DeferredEnqueue(_selectedTrackId));
        }

        IEnumerator DeferredEnqueue(string trackId)
        {
            yield return null;
            if (string.IsNullOrEmpty(trackId)) yield break;
            var queue = TrackPreparationQueue.Instance;
            if (queue != null && !TrackCache.Instance.IsReady(trackId))
                queue.Enqueue(trackId);
        }

        void RefreshTrackListState()
        {
            if (_alefyTracks == null || _trackRows.Count != TracksPerPage) return;

            var queue = TrackPreparationQueue.Instance;
            for (int row = 0; row < TracksPerPage; row++)
            {
                int globalIndex = _currentPage * TracksPerPage + row;
                var go = _trackRows[row];
                if (go == null || !go.activeInHierarchy) continue;
                if (globalIndex >= _alefyTracks.Length) continue;
                var meta = _alefyTracks[globalIndex];
                if (meta == null) continue;
                Text titleLabel = null;
                foreach (var t in go.GetComponentsInChildren<Text>(true))
                {
                    if (t.gameObject.name != "DurationText")
                    {
                        titleLabel = t;
                        break;
                    }
                }
                if (titleLabel != null && queue != null)
                {
                    var best = HighScoreService.GetHighScore(meta.Id);
                    var bestStr = best > 0 ? " | Best: " + best : "";
                    titleLabel.text = meta.Title + " - " + meta.Artist + "\n" + queue.GetStateLabel(meta.Id) + bestStr;
                }

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var colors = btn.colors;
                    colors.normalColor = _selectedIndex == globalIndex ? SelectedColor : NormalColor;
                    btn.colors = colors;
                }
            }
            UpdatePaginationButtons();
        }

        void UpdateStartButton()
        {
            if (ButtonStartGame != null)
                ButtonStartGame.interactable = !string.IsNullOrEmpty(_selectedTrackId) && TrackCache.Instance.IsReady(_selectedTrackId);
        }

        void OnStartGame()
        {
            if (string.IsNullOrEmpty(_selectedTrackId) || !TrackCache.Instance.IsReady(_selectedTrackId))
            {
                SetStatus("Seleziona una traccia e attendi che sia Pronto.");
                return;
            }

            var entry = TrackCache.Instance.GetReady(_selectedTrackId);
            if (entry == null)
            {
                SetStatus("Traccia non più disponibile in cache.");
                return;
            }

#if UNITY_EDITOR
            // #region agent log
            BeatSaberAlefy.UI.DebugLog.Write("MenuController.OnStartGame", "Before LoadScene", "H1 H5",
                ("ClipNull", entry.Clip == null),
                ("RhythmNull", entry.Rhythm == null),
                ("BeatMapNull", entry.BeatMap == null),
                ("EntriesLength", entry.BeatMap?.Entries?.Length ?? -1),
                ("TrackId", _selectedTrackId));
            // #endregion
#endif
            GameSessionData.Set(entry.Clip, entry.Rhythm, entry.BeatMap);
            GameSessionData.SelectedTrackId = _selectedTrackId;
            SceneManager.LoadScene(GameSceneName);
        }

        void SetStatus(string text)
        {
            if (StatusText != null) StatusText.text = text;
        }

        void ResolveTrackListContainer()
        {
            if (AlefyTrackListContainer == null)
            {
                var go = GameObject.Find("TrackListContainer");
                if (go != null)
                    AlefyTrackListContainer = go.transform;
                else
                {
                    var content = GameObject.Find("TrackListContent");
                    if (content != null)
                        AlefyTrackListContainer = content.transform;
                }
            }
            if (AlefyTrackListContainer != null && AlefyTrackListContainer.GetComponent<Image>() == null)
            {
                var img = AlefyTrackListContainer.gameObject.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.18f, 0.92f);
                img.raycastTarget = true;
            }
        }
    }
}
