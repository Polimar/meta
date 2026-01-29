using System;
using UnityEngine;

namespace BeatSaberAlefy.BeatMap
{
    /// <summary>
    /// Beat map completa: lista di note da spawnare, associata a un audio.
    /// </summary>
    [Serializable]
    public class BeatMapData
    {
        public BeatMapEntry[] Entries = Array.Empty<BeatMapEntry>();
        [Tooltip("Durata audio in secondi")]
        public float AudioDurationSeconds;
    }
}
