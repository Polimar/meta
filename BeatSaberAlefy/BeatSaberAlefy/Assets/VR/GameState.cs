using UnityEngine;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Stato di partita: punteggio, vita, miss totali/consecutivi, game over.
    /// Singleton usato da Sliceable (OnHit) e SpawnController (OnMiss).
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public const int MaxTotalMisses = 20;
        public const int MaxConsecutiveMisses = 7;
        public const int PointsPerHit = 100;
        public const int MaxLife = 100;
        public const int LifePerMiss = 5;

        static GameState _instance;
        public static GameState Instance
        {
            get => _instance;
            set => _instance = value;
        }

        public int Score { get; private set; }
        public int Life { get; private set; }
        public int TotalMisses { get; private set; }
        public int ConsecutiveMisses { get; private set; }
        public bool IsGameOver { get; private set; }
        public int ComboCount { get; private set; }

        public const int ComboThreshold2 = 5;
        public const int ComboThreshold4 = 10;

        public int GetComboMultiplier()
        {
            if (ComboCount >= ComboThreshold4) return 4;
            if (ComboCount >= ComboThreshold2) return 2;
            return 1;
        }

        public void Reset()
        {
            Score = 0;
            Life = MaxLife;
            TotalMisses = 0;
            ConsecutiveMisses = 0;
            IsGameOver = false;
            ComboCount = 0;
        }

        public void OnHit()
        {
            if (IsGameOver) return;
            ConsecutiveMisses = 0;
            ComboCount++;
            Score += PointsPerHit * GetComboMultiplier();
        }

        public void OnMiss()
        {
            if (IsGameOver) return;
            ComboCount = 0;
            TotalMisses++;
            ConsecutiveMisses++;
            Life = Mathf.Max(0, Life - LifePerMiss);

            if (TotalMisses >= MaxTotalMisses || ConsecutiveMisses >= MaxConsecutiveMisses)
                IsGameOver = true;
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            Reset();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
