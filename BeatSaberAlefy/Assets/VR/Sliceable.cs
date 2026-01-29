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
        public class SliceEvent : UnityEvent<Vector3 worldCutNormal, int lane> { }

        [Tooltip("Invocato quando il cubo viene tagliato validamente")]
        public SliceEvent OnSliced = new SliceEvent();

        [Tooltip("Finestra di tempo in secondi (es. 0.2 = ±200 ms) per considerare lo slice valido")]
        public float TimingWindowSeconds = 0.2f;

        [Tooltip("Se true, il cubo viene disattivato dopo lo slice")]
        public bool DisableOnSlice = true;

        bool _sliced;

        /// <summary>
        /// Chiamato da SliceDetector quando un saber attraversa il cubo.
        /// Restituisce true se lo slice è valido (timing e opzionalmente direzione).
        /// </summary>
        public bool TrySlice(float currentAudioTime, Vector3 cutPlaneNormal, int saberLane)
        {
            if (_sliced) return false;
            float delta = Mathf.Abs(currentAudioTime - BeatTime);
            if (delta > TimingWindowSeconds) return false;
            if (saberLane >= 0 && Lane >= 0 && saberLane != Lane) return false;
            _sliced = true;

            Vector3 pos = transform.position;
            if (SliceParticlesPrefab != null)
            {
                var fx = Object.Instantiate(SliceParticlesPrefab, pos, Quaternion.identity);
                Object.Destroy(fx, 2f);
            }
            if (SliceSound != null)
            {
                AudioSource.PlayClipAtPoint(SliceSound, pos);
            }

            OnSliced?.Invoke(cutPlaneNormal, Lane);
            if (DisableOnSlice)
                gameObject.SetActive(false);
            return true;
        }

        public void ResetSliceState()
        {
            _sliced = false;
        }
    }
}
