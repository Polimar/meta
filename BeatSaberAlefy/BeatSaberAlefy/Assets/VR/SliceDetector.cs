using UnityEngine;
using System.Collections.Generic;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Attaccato a ogni spada laser. Rileva collisioni con cubi Sliceable e notifica il slice.
    /// Richiede un Collider (trigger) sulla spada e un riferimento al tempo audio corrente.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SliceDetector : MonoBehaviour
    {
        [Tooltip("Lane di questa spada: 0 = sinistra, 1 = destra, -1 = ignora validazione lane")]
        public int SaberLane = -1;

        [Tooltip("Riferimento al componente che fornisce il tempo audio corrente (es. GameplayDirector)")]
        public MonoBehaviour AudioTimeProvider;

        [Tooltip("Nome del metodo o proprietà che restituisce float (tempo in secondi). Es. GetAudioTime")]
        public string AudioTimeMethodName = "GetAudioTime";

        System.Reflection.MethodInfo _audioTimeMethod;
        System.Func<float> _getAudioTime;

        void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger) col.isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            ResolveAudioTime();
        }

        void ResolveAudioTime()
        {
            if (AudioTimeProvider == null) return;
            var type = AudioTimeProvider.GetType();
            var method = type.GetMethod(AudioTimeMethodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null && method.ReturnType == typeof(float))
            {
                _audioTimeMethod = method;
                return;
            }
            var prop = type.GetProperty(AudioTimeMethodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(float))
            {
                _getAudioTime = () => (float)prop.GetValue(AudioTimeProvider);
                return;
            }
        }

        float GetCurrentAudioTime()
        {
            if (_getAudioTime != null) return _getAudioTime();
            if (_audioTimeMethod != null && AudioTimeProvider != null)
                return (float)_audioTimeMethod.Invoke(AudioTimeProvider, null);
            return 0f;
        }

        void OnTriggerEnter(Collider other)
        {
            TrySlice(other);
        }

        void OnTriggerStay(Collider other)
        {
            TrySlice(other);
        }

        void TrySlice(Collider other)
        {
            var sliceable = other.GetComponent<Sliceable>();
            if (sliceable == null) sliceable = other.GetComponentInParent<Sliceable>();
            if (sliceable == null) return;
            Vector3 cutNormal = transform.forward;
            Vector3 cutPosition = other.ClosestPoint(transform.position);
            float t = GetCurrentAudioTime();
            sliceable.TrySlice(t, cutPosition, cutNormal, SaberLane);
        }
    }
}
