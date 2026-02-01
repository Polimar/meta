using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.Alefy;

namespace BeatSaberAlefy.UI
{
    public enum TrackPreparationState
    {
        NotInQueue,
        Queued,
        Downloading,
        Analyzing,
        Ready,
        Error
    }

    public class TrackPreparationJob
    {
        public string TrackId;
        public TrackPreparationState State;
        public string ErrorMessage;
    }

    /// <summary>
    /// Coda di preparazione tracce: download + load + analisi ritmo. Un job alla volta, in background.
    /// </summary>
    public class TrackPreparationQueue : MonoBehaviour
    {
        static TrackPreparationQueue _instance;
        public static TrackPreparationQueue Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("TrackPreparationQueue");
                _instance = go.AddComponent<TrackPreparationQueue>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        readonly Queue<TrackPreparationJob> _queue = new Queue<TrackPreparationJob>();
        readonly Dictionary<string, TrackPreparationJob> _jobsByTrackId = new Dictionary<string, TrackPreparationJob>();
        bool _workerRunning;
        IAlefyClient _alefyClient;

        public IAlefyClient AlefyClient
        {
            get => _alefyClient;
            set => _alefyClient = value;
        }

        public void Enqueue(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            var key = trackId.Trim();
            if (TrackCache.Instance.IsReady(key)) return;
            if (_jobsByTrackId.ContainsKey(key)) return;

            var job = new TrackPreparationJob { TrackId = key, State = TrackPreparationState.Queued };
            _queue.Enqueue(job);
            _jobsByTrackId[key] = job;

            if (!_workerRunning)
                StartCoroutine(WorkerCoroutine());
        }

        public TrackPreparationState GetState(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return TrackPreparationState.NotInQueue;
            var key = trackId.Trim();
            if (TrackCache.Instance.IsReady(key)) return TrackPreparationState.Ready;
            if (_jobsByTrackId.TryGetValue(key, out var job)) return job.State;
            return TrackPreparationState.NotInQueue;
        }

        public string GetStateLabel(string trackId)
        {
            switch (GetState(trackId))
            {
                case TrackPreparationState.Ready: return "Pronto";
                case TrackPreparationState.Queued: return "In coda";
                case TrackPreparationState.Downloading: return "Download…";
                case TrackPreparationState.Analyzing: return "Analisi…";
                case TrackPreparationState.Error: return "Errore";
                default: return "";
            }
        }

        IEnumerator WorkerCoroutine()
        {
            _workerRunning = true;
            try
            {
                while (_queue.Count > 0)
                {
                    var job = _queue.Dequeue();
                    if (TrackCache.Instance.IsReady(job.TrackId))
                    {
                        _jobsByTrackId.Remove(job.TrackId);
                        continue;
                    }

                    job.State = TrackPreparationState.Downloading;
                    _jobsByTrackId[job.TrackId] = job;

                    AlefySongResult prepareResult = null;
                    var svc = _alefyClient as AlefyService;
                    if (svc != null)
                    {
                        yield return svc.PrepareSongAsyncCoroutine(job.TrackId, r => prepareResult = r);
                    }
                    else
                    {
                        job.State = TrackPreparationState.Error;
                        job.ErrorMessage = "Client Alefy non disponibile";
                        _jobsByTrackId.Remove(job.TrackId);
                        continue;
                    }

                    if (prepareResult == null || string.IsNullOrEmpty(prepareResult.LocalAudioPath))
                    {
                        job.State = TrackPreparationState.Error;
                        job.ErrorMessage = "Download fallito";
                        _jobsByTrackId.Remove(job.TrackId);
                        continue;
                    }

                    bool loadDone = false;
                    AudioClip clip = null;
                    string loadError = null;
                    yield return AudioClipLoader.LoadFromFile(prepareResult.LocalAudioPath, c => { clip = c; loadDone = true; }, e => { loadError = e; loadDone = true; });
                    yield return new WaitUntil(() => loadDone);

                    if (clip == null)
                    {
                        job.State = TrackPreparationState.Error;
                        job.ErrorMessage = loadError ?? "Caricamento audio fallito";
                        _jobsByTrackId.Remove(job.TrackId);
                        continue;
                    }

                    job.State = TrackPreparationState.Analyzing;
                    RhythmData rhythm = null;
                    yield return StartCoroutine(RhythmAnalyzer.AnalyzeAsync(clip, r => rhythm = r));

                    if (rhythm == null)
                    {
                        job.State = TrackPreparationState.Error;
                        job.ErrorMessage = "Analisi ritmo fallita";
                        _jobsByTrackId.Remove(job.TrackId);
                        continue;
                    }

                    var beatMap = BeatMapGenerator.Generate(rhythm, clip.length, BeatMapGenerator.DefaultBeatsPerNote, 2, 3);
                    var entry = new CachedTrack
                    {
                        Id = job.TrackId,
                        Metadata = prepareResult.Metadata,
                        Clip = clip,
                        Rhythm = rhythm,
                        BeatMap = beatMap
                    };
#if UNITY_EDITOR
                    // #region agent log
                    DebugLog.Write("TrackPreparationQueue.Worker", "SetReady", "H5",
                        ("TrackId", job.TrackId),
                        ("EntriesLength", beatMap?.Entries?.Length ?? -1),
                        ("ClipLength", clip?.length ?? -1f));
                    // #endregion
#endif
                    TrackCache.Instance.SetReady(job.TrackId, entry);
                    _jobsByTrackId.Remove(job.TrackId);
                }
            }
            finally
            {
                _workerRunning = false;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
