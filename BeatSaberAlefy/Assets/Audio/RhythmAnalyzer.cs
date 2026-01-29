using System.Collections;
using UnityEngine;
using BeatSaberAlefy.BeatMap;

namespace BeatSaberAlefy.Audio
{
    /// <summary>
    /// Analizza un AudioClip e produce RhythmData (BPM, offset, opzionale lista beat).
    /// Implementazione semplificata: stima BPM da picchi di energia (spectral flux–like) e offset prima battuta.
    /// Per produzione si può integrare AudioSync-style o onset detection più preciso.
    /// </summary>
    public static class RhythmAnalyzer
    {
        const int SampleBlockSize = 1024;
        const float MinBPM = 60f;
        const float MaxBPM = 200f;
        const int BPMSteps = 140;

        /// <summary>
        /// Analizza l'audio e restituisce RhythmData. Esecuzione sincrona (può richiedere qualche secondo).
        /// </summary>
        public static RhythmData Analyze(AudioClip clip)
        {
            if (clip == null) return new RhythmData { BPM = 120f, FirstBeatOffsetSeconds = 0f };

            int samples = clip.samples * clip.channels;
            float[] data = new float[samples];
            clip.GetData(data, 0);

            float sampleRate = clip.frequency;
            float duration = clip.length;

            // Energia per blocco (RMS)
            int numBlocks = samples / SampleBlockSize;
            float[] energy = new float[numBlocks];
            for (int i = 0; i < numBlocks; i++)
            {
                float sum = 0f;
                for (int j = 0; j < SampleBlockSize; j++)
                {
                    float s = data[i * SampleBlockSize + j];
                    sum += s * s;
                }
                energy[i] = Mathf.Sqrt(sum / SampleBlockSize);
            }

            // Spectral flux–like: differenza tra blocchi consecutivi (onset strength)
            float[] onset = new float[numBlocks];
            for (int i = 1; i < numBlocks; i++)
                onset[i] = Mathf.Max(0f, energy[i] - energy[i - 1]);

            // Smoothing leggero
            float[] smoothed = new float[numBlocks];
            int smooth = 3;
            for (int i = 0; i < numBlocks; i++)
            {
                float sum = 0f;
                int count = 0;
                for (int j = Mathf.Max(0, i - smooth); j <= Mathf.Min(numBlocks - 1, i + smooth); j++)
                {
                    sum += onset[j];
                    count++;
                }
                smoothed[i] = count > 0 ? sum / count : 0f;
            }

            // Trova BPM: autocorrelazione su onset (cerca periodo che massimizza overlap)
            float blockDuration = (float)SampleBlockSize / clip.channels / sampleRate;
            float bestBPM = 120f;
            float bestScore = 0f;

            for (int step = 0; step < BPMSteps; step++)
            {
                float bpm = MinBPM + step * (MaxBPM - MinBPM) / BPMSteps;
                float periodBlocks = (60f / bpm) / blockDuration;
                if (periodBlocks < 2) continue;

                int period = Mathf.RoundToInt(periodBlocks);
                float score = 0f;
                int hits = 0;
                for (int i = period; i < numBlocks - period; i += Mathf.Max(1, period / 2))
                {
                    score += smoothed[i] * smoothed[i - period];
                    hits++;
                }
                if (hits > 0) score /= hits;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestBPM = bpm;
                }
            }

            // Offset prima battuta: primo picco significativo negli onset
            float threshold = 0f;
            for (int i = 0; i < numBlocks; i++)
                threshold += smoothed[i];
            threshold /= numBlocks;
            threshold *= 1.2f;

            int firstBeatBlock = 0;
            for (int i = 1; i < numBlocks - 1; i++)
            {
                if (smoothed[i] > threshold && smoothed[i] > smoothed[i - 1] && smoothed[i] > smoothed[i + 1])
                {
                    firstBeatBlock = i;
                    break;
                }
            }

            float firstBeatOffset = firstBeatBlock * blockDuration;

            // Genera lista beat times (opzionale)
            int beatCount = Mathf.FloorToInt((duration - firstBeatOffset) * bestBPM / 60f);
            float[] beatTimes = new float[beatCount];
            for (int i = 0; i < beatCount; i++)
                beatTimes[i] = firstBeatOffset + i * 60f / bestBPM;

            return new RhythmData
            {
                BPM = bestBPM,
                FirstBeatOffsetSeconds = firstBeatOffset,
                BeatTimesSeconds = beatTimes
            };
        }

        /// <summary>
        /// Wrapper coroutine per analisi asincrona (evita freeze UI).
        /// </summary>
        public static IEnumerator AnalyzeAsync(AudioClip clip, System.Action<RhythmData> onComplete)
        {
            var result = Analyze(clip);
            onComplete?.Invoke(result);
            yield return null;
        }
    }
}
