using UnityEngine;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Frammento di slice: si muove in una direzione e viene distrutto dopo un tempo.
    /// Usato da Sliceable per l'effetto visivo del taglio.
    /// </summary>
    public class SliceDebris : MonoBehaviour
    {
        public Vector3 Velocity;
        public float Lifetime = 1.2f;
        float _t;

        void Update()
        {
            transform.position += Velocity * Time.deltaTime;
            _t += Time.deltaTime;
            if (_t >= Lifetime)
                Destroy(gameObject);
        }
    }
}
