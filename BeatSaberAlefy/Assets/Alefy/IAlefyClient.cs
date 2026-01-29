using System;
using System.Threading.Tasks;

namespace BeatSaberAlefy.Alefy
{
    /// <summary>
    /// Metadati di una canzone restituiti dall'API alefy.
    /// </summary>
    [Serializable]
    public class AlefySongMetadata
    {
        public string Id;
        public string Title;
        public string Artist;
        public float? BPM;
        public string AudioUrl;
        public float? DurationSeconds;
    }

    /// <summary>
    /// Risultato del download/preparazione di una canzone: percorso file locale + metadati.
    /// </summary>
    [Serializable]
    public class AlefySongResult
    {
        public string LocalAudioPath;
        public AlefySongMetadata Metadata;
    }

    /// <summary>
    /// Client per l'API alefy.alevale.it.
    /// Da implementare con la documentazione e le credenziali fornite.
    /// </summary>
    public interface IAlefyClient
    {
        /// <summary>
        /// Ottiene l'elenco delle canzoni disponibili (o ricerca).
        /// </summary>
        Task<AlefySongMetadata[]> GetSongsAsync(string searchQuery = null);

        /// <summary>
        /// Ottiene i dettagli e l'URL audio di una canzone per id.
        /// </summary>
        Task<AlefySongMetadata> GetSongByIdAsync(string songId);

        /// <summary>
        /// Scarica o streama l'audio della canzone e restituisce il percorso del file locale.
        /// </summary>
        Task<AlefySongResult> PrepareSongAsync(string songId);
    }
}
