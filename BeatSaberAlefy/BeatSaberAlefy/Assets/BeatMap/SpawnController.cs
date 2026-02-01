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
        int _logTicks;

        static float GetSafeAudioTime(AudioSource src)
        {
            if (src == null || src.clip == null) return 0f;
            if (src.clip.loadState != AudioDataLoadState.Loaded) return 0f;
            return src.time;
        }

        void Update()
        {
#if UNITY_EDITOR
            _logTicks++;
            if (_logTicks == 60 || (_logTicks <= 5 && _logTicks % 1 == 0))
            {
                float audioTime = AudioSource != null ? GetSafeAudioTime(AudioSource) : -1f;
                BeatSaberAlefy.UI.DebugLog.Write("SpawnController.Update", "Spawn state", "H2 H3 H4",
                    ("BeatMapNull", BeatMap == null),
                    ("EntriesLength", BeatMap?.Entries?.Length ?? -1),
                    ("CubePrefabNull", CubePrefab == null),
                    ("AudioSourceNull", AudioSource == null),
                    ("AudioTime", audioTime),
                    ("NextIndex", _nextIndex),
                    ("AudioPlaying", AudioSource?.isPlaying ?? false));
            }
#endif
            if (BeatMap == null || BeatMap.Entries == null || CubePrefab == null || AudioSource == null || AudioSource.clip == null)
                return;

            float t = GetSafeAudioTime(AudioSource);

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

            Vector3 playerPos = PlayerForward != null ? PlayerForward.position : transform.position;
            float laneOffset = (entry.Lane == 0 ? -1f : 1f) * LaneOffset;
            float heightOffset = GetHeightOffset(entry.Height);

            Vector3 hitPos = playerPos + forward * HitDistance;
            hitPos.y += heightOffset;
            hitPos += right * laneOffset;

            Vector3 spawnPos = playerPos + forward * SpawnDistance;
            spawnPos.y += heightOffset;
            spawnPos += right * laneOffset;

            GameObject cube = Instantiate(CubePrefab, spawnPos, Quaternion.identity, transform);
            var rb = cube.GetComponent<Rigidbody>();
            if (rb == null) rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var sliceable = cube.GetComponent<Sliceable>();
            if (sliceable != null)
            {
                sliceable.BeatTime = entry.Time;
                sliceable.Lane = entry.Lane;
            }
            var rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                Color laneColor = entry.Lane == 0 ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.3f, 0.6f);
                var block = new MaterialPropertyBlock();
                rend.GetPropertyBlock(block);
                block.SetColor("_BaseColor", laneColor);
                block.SetColor("_Color", laneColor);
                rend.SetPropertyBlock(block);
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
                    var sliceable = go.GetComponent<Sliceable>();
                    if (sliceable != null && !sliceable.Sliced && GameState.Instance != null)
                        GameState.Instance.OnMiss();
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
            return GetSafeAudioTime(AudioSource);
        }
    }
}
