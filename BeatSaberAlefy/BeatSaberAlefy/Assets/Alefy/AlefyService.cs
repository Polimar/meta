using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaberAlefy.Alefy
{
    /// <summary>
    /// Implementazione di IAlefyClient per alefy.alevale.it.
    /// BaseUrl e AuthToken da AlefySettings (Resources/Settings/AlefySettings).
    /// API: GET /api/tracks, GET /api/tracks/:id, GET /api/stream/tracks/:id (vedi EXTERNAL_API.md).
    /// </summary>
    public class AlefyService : IAlefyClient
    {
        public string BaseUrl = "https://alefy.alevale.it";
        public string AuthToken;

        readonly string _cacheDir;

        public AlefyService()
        {
            _cacheDir = Path.Combine(Application.temporaryCachePath, "AlefyCache");
            if (!Directory.Exists(_cacheDir))
                Directory.CreateDirectory(_cacheDir);
        }

        string Url(string path)
        {
            path = path.TrimStart('/');
            return BaseUrl.EndsWith("/") ? BaseUrl + path : BaseUrl + "/" + path;
        }

        void SetAuth(UnityWebRequest req)
        {
            if (!string.IsNullOrEmpty(AuthToken))
                req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
        }

        static AlefySongMetadata TrackToMetadata(AlefyTrackDto t, string baseUrl)
        {
            if (t == null) return null;
            var baseUrlTrimmed = baseUrl.TrimEnd('/');
            return new AlefySongMetadata
            {
                Id = t.id.ToString(),
                Title = t.title ?? "",
                Artist = t.artist ?? "",
                Album = t.album ?? "",
                DurationSeconds = t.duration,
                AudioUrl = baseUrlTrimmed + "/api/stream/tracks/" + t.id,
                CoverArtUrl = baseUrlTrimmed + "/api/stream/tracks/" + t.id + "/cover"
            };
        }

        /// <summary>
        /// Coroutine: ottiene l'elenco tracce (main-thread safe).
        /// </summary>
        public IEnumerator GetSongsAsyncCoroutine(string searchQuery, Action<AlefySongMetadata[]> onComplete)
        {
            var query = "limit=100";
            if (!string.IsNullOrEmpty(searchQuery))
                query += "&search=" + Uri.EscapeDataString(searchQuery);
            string path = "api/tracks?" + query;

            using (var req = UnityWebRequest.Get(Url(path)))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    yield return null;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Alefy GET {path}: {req.error} {req.downloadHandler?.text}");
                    onComplete?.Invoke(Array.Empty<AlefySongMetadata>());
                    yield break;
                }

                string json = req.downloadHandler?.text;
                if (string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(Array.Empty<AlefySongMetadata>());
                    yield break;
                }

                var resp = JsonUtility.FromJson<AlefyTracksResponse>(json);
                if (resp == null || !resp.success || resp.data?.tracks == null)
                {
                    var err = JsonUtility.FromJson<AlefyErrorResponse>(json);
                    if (err?.error != null)
                        Debug.LogWarning("Alefy: " + err.error.message);
                    onComplete?.Invoke(Array.Empty<AlefySongMetadata>());
                    yield break;
                }

                string baseUrl = BaseUrl.TrimEnd('/');
                var list = new AlefySongMetadata[resp.data.tracks.Length];
                for (int i = 0; i < resp.data.tracks.Length; i++)
                    list[i] = TrackToMetadata(resp.data.tracks[i], baseUrl);
                onComplete?.Invoke(list);
            }
        }

        public System.Threading.Tasks.Task<AlefySongMetadata[]> GetSongsAsync(string searchQuery = null)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<AlefySongMetadata[]>();
            // L'editor deve avviare la coroutine; da codice chiamare GetSongsAsyncCoroutine con un runner
            tcs.SetResult(Array.Empty<AlefySongMetadata>());
            return tcs.Task;
        }

        /// <summary>
        /// Coroutine: dettaglio traccia (main-thread safe).
        /// </summary>
        public IEnumerator GetSongByIdAsyncCoroutine(string songId, Action<AlefySongMetadata> onComplete)
        {
            if (string.IsNullOrEmpty(songId))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            string path = "api/tracks/" + songId.Trim();
            using (var req = UnityWebRequest.Get(Url(path)))
            {
                SetAuth(req);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    yield return null;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Alefy GET {path}: {req.error}");
                    onComplete?.Invoke(null);
                    yield break;
                }

                string json = req.downloadHandler?.text;
                if (string.IsNullOrEmpty(json))
                {
                    onComplete?.Invoke(null);
                    yield break;
                }

                var resp = JsonUtility.FromJson<AlefyTrackDetailResponse>(json);
                if (resp == null || !resp.success || resp.data?.track == null)
                {
                    var err = JsonUtility.FromJson<AlefyErrorResponse>(json);
                    if (err?.error != null)
                        Debug.LogWarning("Alefy: " + err.error.message);
                    onComplete?.Invoke(null);
                    yield break;
                }

                onComplete?.Invoke(TrackToMetadata(resp.data.track, BaseUrl.TrimEnd('/')));
            }
        }

        public System.Threading.Tasks.Task<AlefySongMetadata> GetSongByIdAsync(string songId)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<AlefySongMetadata>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        /// <summary>
        /// Coroutine: scarica audio e restituisce percorso locale (main-thread safe).
        /// </summary>
        public IEnumerator PrepareSongAsyncCoroutine(string songId, Action<AlefySongResult> onComplete)
        {
            AlefySongMetadata metadata = null;
            yield return GetSongByIdAsyncCoroutine(songId, m => metadata = m);
            if (metadata == null)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            string localPath = Path.Combine(_cacheDir, songId.Trim() + ".mp3");
            if (File.Exists(localPath))
            {
                onComplete?.Invoke(new AlefySongResult { LocalAudioPath = localPath, Metadata = metadata });
                yield break;
            }

            string path = "api/stream/tracks/" + songId.Trim() + "?download=1";
            using (var req = UnityWebRequest.Get(Url(path)))
            {
                SetAuth(req);
                req.downloadHandler = new DownloadHandlerFile(localPath);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    yield return null;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Alefy stream {path}: {req.error}");
                    onComplete?.Invoke(null);
                    yield break;
                }
            }

            onComplete?.Invoke(new AlefySongResult { LocalAudioPath = localPath, Metadata = metadata });
        }

        public System.Threading.Tasks.Task<AlefySongResult> PrepareSongAsync(string songId)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<AlefySongResult>();
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}
