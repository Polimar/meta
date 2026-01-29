using System;
using UnityEngine;

namespace BeatSaberAlefy.BeatMap
{
    /// <summary>
    /// Una "nota" della beat map: tempo, lane (sinistra/destra), altezza (alto/centro/basso).
    /// </summary>
    [Serializable]
    public class BeatMapEntry
    {
        [Tooltip("Tempo in secondi dall'inizio dell'audio")]
        public float Time;

        [Tooltip("Lane: 0 = sinistra, 1 = destra (o -1 = centro)")]
        public int Lane;

        [Tooltip("Altezza: 0 = basso, 1 = centro, 2 = alto")]
        public int Height;

        public BeatMapEntry() { }

        public BeatMapEntry(float time, int lane, int height)
        {
            Time = time;
            Lane = lane;
            Height = height;
        }
    }
}
