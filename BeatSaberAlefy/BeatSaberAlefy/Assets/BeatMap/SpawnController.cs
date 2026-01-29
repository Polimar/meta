using System.Collections.Generic;
using UnityEngine;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.VR;

namespace BeatSaberAlefy.BeatMap
{
    /// <summary>
    /// Sincronizzato con AudioSource: avanza il tempo e per ogni entry della beat map istanzia un cubo
    /// nella lane/altezza corretta. I cubi si muovono verso il player per arrivare "a tempo" nel punto di slice.
    /// </summary>
    public class SpawnController : MonoBehaviour
    {
        [Tooltip("Beat map da usare")]
        public BeatMapData BeatMap;

        [Tooltip("Prefab del cubo da istanziare (deve avere Sliceable + collider)")]
        public GameObject CubePrefab;

        [Tooltip("AudioSource che suona la traccia (tempo da AudioSource.time)")]
        public AudioSource AudioSource;

        [Tooltip("Distanza di spawn davanti al player (metri)")]
        public float SpawnDistance = 8f;

        [Tooltip("Distanza dal player dove il cubo deve essere tagliato (metri)")]
        public float HitDistance = 1.5f;

        [Tooltip("Transform del player/XR Origin per posizione e direzione avanti")]
        public Transform PlayerForward;

        [Tooltip("Offset lane: distanza orizzontale (metri) tra centro e lane sinistra/destra")]
        public float LaneOffset = 0.3f;

        [Tooltip("Altezze per le 3 righe: Y local rispetto al punto di hit")]
        public float[] HeightOffsets = { -0.3f, 0f, 0.3f };

        int _nextIndex;
        readonly List<GameObject> _activeCubes = new List<GameObject>();

        void Update()
        {
            if (BeatMap == null || BeatMap.Entries == null || CubePrefab == null || AudioSource == null)
                return;

            float t = AudioSource.time;

            while (_nextIndex < BeatMap.Entries.Length)
            {
                var entry = BeatMap.Entries[_nextIndex];
                float timeUntilHit = entry.Time - t;
                float travelTime = (SpawnDistance - HitDistance) / GetCubeSpeed();
                if (timeUntilHit > travelTime)
                    break;

                SpawnCube(entry);
                _nextIndex++;
            }

            CleanupPassedCubes(t);
        }

        float GetCubeSpeed()
        {
            float distance = SpawnDistance - HitDistance;
            if (distance <= 0) return 5f;
            float travelTime = 2f;
            return distance / travelTime;
        }

        void SpawnCube(BeatMapEntry entry)
        {
            Vector3 forward = PlayerForward != null ? PlayerForward.forward : Vector3.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = PlayerForward != null ? PlayerForward.right : Vector3.right;
            right.y = 0f;
            right.Normalize();

            Vector3 hitPos = (PlayerForward != null ? PlayerForward.position : transform.position)
                + forward * HitDistance;
            hitPos.y += GetHeightOffset(entry.Height);

            float laneOffset = (entry.Lane == 0 ? -1f : 1f) * LaneOffset;
            hitPos += right * laneOffset;

            Vector3 spawnPos = hitPos - forward * SpawnDistance;
            spawnPos.y = hitPos.y;

            GameObject cube = Instantiate(CubePrefab, spawnPos, Quaternion.identity, transform);
            var sliceable = cube.GetComponent<Sliceable>();
            if (sliceable != null)
            {
                sliceable.BeatTime = entry.Time;
                sliceable.Lane = entry.Lane;
            }

            var mover = cube.GetComponent<CubeMover>();
            if (mover == null) mover = cube.AddComponent<CubeMover>();
            mover.TargetPosition = hitPos;
            mover.Speed = GetCubeSpeed();
            mover.BeatTime = entry.Time;

            _activeCubes.Add(cube);
        }

        float GetHeightOffset(int height)
        {
            if (HeightOffsets != null && height >= 0 && height < HeightOffsets.Length)
                return HeightOffsets[height];
            return 0f;
        }

        void CleanupPassedCubes(float currentTime)
        {
            for (int i = _activeCubes.Count - 1; i >= 0; i--)
            {
                var go = _activeCubes[i];
                if (go == null || !go.activeInHierarchy)
                {
                    _activeCubes.RemoveAt(i);
                    continue;
                }
                var mover = go.GetComponent<CubeMover>();
                if (mover != null && currentTime > mover.BeatTime + 0.5f)
                {
                    _activeCubes.RemoveAt(i);
                    Destroy(go);
                }
            }
        }

        /// <summary>
        /// Resetta lo spawn (chiamare quando si riavvia la partita).
        /// </summary>
        public void ResetSpawn()
        {
            foreach (var go in _activeCubes)
            {
                if (go != null) Destroy(go);
            }
            _activeCubes.Clear();
            _nextIndex = 0;
        }

        /// <summary>
        /// Tempo audio corrente (per SliceDetector / GameplayDirector).
        /// </summary>
        public float GetAudioTime()
        {
            return AudioSource != null ? AudioSource.time : 0f;
        }
    }
}
