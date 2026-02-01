using System;
using System.Collections.Generic;
using UnityEngine;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.Alefy;

namespace BeatSaberAlefy.UI
{
    /// <summary>
    /// Entry in memoria per una traccia pronta: clip, rhythm, beat map.
    /// </summary>
    public class CachedTrack
    {
        public string Id;
        public AlefySongMetadata Metadata;
        public AudioClip Clip;
        public RhythmData Rhythm;
        public BeatMapData BeatMap;
    }

    /// <summary>
    /// Cache tracce pronte in memoria. Singleton con DontDestroyOnLoad per persistenza tra Menu e Game.
    /// </summary>
    public class TrackCache : MonoBehaviour
    {
        static TrackCache _instance;
        public static TrackCache Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("TrackCache");
                _instance = go.AddComponent<TrackCache>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        readonly Dictionary<string, CachedTrack> _cache = new Dictionary<string, CachedTrack>();
        readonly List<string> _insertionOrder = new List<string>();

        [Tooltip("Numero massimo di tracce in cache (0 = illimitato). Oltre questo limite si rimuove la più vecchia (FIFO).")]
        public int MaxCachedTracks = 10;

        public bool IsReady(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return false;
            return _cache.ContainsKey(trackId.Trim());
        }

        public CachedTrack GetReady(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return null;
            var key = trackId.Trim();
            return _cache.TryGetValue(key, out var entry) ? entry : null;
        }

        public void SetReady(string trackId, CachedTrack entry)
        {
            if (string.IsNullOrEmpty(trackId) || entry == null) return;
            var key = trackId.Trim();
            if (!_cache.ContainsKey(key))
                _insertionOrder.Add(key);
            _cache[key] = entry;

            while (MaxCachedTracks > 0 && _insertionOrder.Count > MaxCachedTracks)
            {
                var oldest = _insertionOrder[0];
                _insertionOrder.RemoveAt(0);
                _cache.Remove(oldest);
            }
        }

        public void Clear()
        {
            _cache.Clear();
            _insertionOrder.Clear();
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
