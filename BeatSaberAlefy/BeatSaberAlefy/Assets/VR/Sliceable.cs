using UnityEngine;
using UnityEngine.Events;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Attaccato a ogni cubo. Rileva slice da parte di un saber (SliceDetector) e invoca evento con direzione/timing.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Sliceable : MonoBehaviour
    {
        [Tooltip("Tempo in secondi del beat associato a questo cubo (per timing window)")]
        public float BeatTime;

        [Tooltip("Lane del cubo (0 sinistra, 1 destra) per validare la mano")]
        public int Lane;

        [Tooltip("Prefab particelle da istanziare al punto di slice (opzionale)")]
        public GameObject SliceParticlesPrefab;

        [Tooltip("AudioClip da riprodurre allo slice (opzionale)")]
        public AudioClip SliceSound;

        [System.Serializable]
        public class SliceEvent : UnityEvent<Vector3, Vector3, int> { }

        [Tooltip("Invocato quando il cubo viene tagliato validamente (cutNormal, cutPosition, lane)")]
        public SliceEvent OnSliced = new SliceEvent();

        [Tooltip("Finestra di tempo in secondi (es. 0.2 = ±200 ms) per considerare lo slice valido")]
        public float TimingWindowSeconds = 0.2f;

        [Tooltip("Se true, il cubo viene disattivato dopo lo slice")]
        public bool DisableOnSlice = true;

        bool _sliced;

        public bool Sliced => _sliced;

        /// <summary>
        /// Chiamato da SliceDetector quando un saber attraversa il cubo.
        /// cutPosition = punto di impatto sul cubo, cutPlaneNormal = direzione del taglio.
        /// Restituisce true se lo slice è valido (timing e opzionalmente direzione).
        /// </summary>
        [Tooltip("Distanza massima dal punto di taglio (TargetPosition del CubeMover) per considerare lo slice valido")]
        public float HitZoneDistance = 1.2f;

        public bool TrySlice(float currentAudioTime, Vector3 cutPosition, Vector3 cutPlaneNormal, int saberLane)
        {
            if (_sliced) return false;
            var mover = GetComponent<BeatSaberAlefy.BeatMap.CubeMover>();
            bool inZone = false;
            if (mover != null)
            {
                float dist = Vector3.Distance(transform.position, mover.TargetPosition);
                inZone = dist <= HitZoneDistance;
                if (!inZone)
                    return false;
            }
            if (!inZone)
            {
                float delta = Mathf.Abs(currentAudioTime - BeatTime);
                if (delta > TimingWindowSeconds)
                    return false;
            }
            if (saberLane >= 0 && Lane >= 0 && saberLane != Lane)
                return false;
            _sliced = true;

            if (GameState.Instance != null)
                GameState.Instance.OnHit();

            if (SliceParticlesPrefab != null)
            {
                var fx = Object.Instantiate(SliceParticlesPrefab, cutPosition, Quaternion.LookRotation(cutPlaneNormal));
                Object.Destroy(fx, 2f);
            }
            if (SliceSound != null)
                AudioSource.PlayClipAtPoint(SliceSound, cutPosition);

            SpawnPhysicsSlice(cutPosition, cutPlaneNormal);

            OnSliced?.Invoke(cutPlaneNormal, cutPosition, Lane);
            if (DisableOnSlice)
                StartCoroutine(DisableCubeNextFrame());
            return true;
        }

        [Tooltip("Forza impressa alle due metà dopo il taglio (impulso)")]
        public float SliceForceMagnitude = 6f;
        [Tooltip("Tempo prima che le metà vengano distrutte")]
        public float SliceHalvesLifetime = 2f;

        void SpawnPhysicsSlice(Vector3 cutPosition, Vector3 cutPlaneNormal)
        {
            Color tint = Lane == 0 ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.3f, 0.6f);
            var mr = GetComponentInChildren<MeshRenderer>();
            Material mat = (mr != null && mr.sharedMaterial != null) ? mr.sharedMaterial : null;
            // Piano di taglio per il centro del cubo così si ottengono sempre due metà (cutPosition può essere sulla superficie e dare triB=0)
            Vector3 planeThroughCenter = transform.position;
            MeshSlice.SliceAndSpawn(transform, planeThroughCenter, cutPlaneNormal, mat, tint, SliceForceMagnitude, SliceHalvesLifetime);
        }

        public void ResetSliceState()
        {
            _sliced = false;
        }

        System.Collections.IEnumerator DisableCubeNextFrame()
        {
            yield return null;
            if (this != null && gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
