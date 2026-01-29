using UnityEngine;

namespace BeatSaberAlefy.BeatMap
{
    /// <summary>
    /// Muove il cubo verso TargetPosition a velocità costante (usato da SpawnController).
    /// </summary>
    public class CubeMover : MonoBehaviour
    {
        public Vector3 TargetPosition;
        public float Speed = 5f;
        [HideInInspector] public float BeatTime;

        void Update()
        {
            Vector3 diff = TargetPosition - transform.position;
            float step = Speed * Time.deltaTime;
            if (diff.sqrMagnitude <= step * step)
            {
                transform.position = TargetPosition;
                return;
            }
            transform.position += diff.normalized * step;
        }
    }
}
