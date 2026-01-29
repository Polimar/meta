using System;

namespace BeatSaberAlefy.Alefy
{
    /// <summary>
    /// DTO per risposta GET /api/tracks (lista con paginazione).
    /// Nomi campi come nell'API (snake_case) per JsonUtility.
    /// </summary>
    [Serializable]
    public class AlefyTracksResponse
    {
        public bool success;
        public AlefyTracksData data;
    }

    [Serializable]
    public class AlefyTracksData
    {
        public AlefyTrackDto[] tracks;
        public AlefyPagination pagination;
    }

    [Serializable]
    public class AlefyPagination
    {
        public int page;
        public int limit;
        public int total;
        public int totalPages;
    }

    /// <summary>
    /// DTO per risposta GET /api/tracks/:id (singola traccia).
    /// </summary>
    [Serializable]
    public class AlefyTrackDetailResponse
    {
        public bool success;
        public AlefyTrackDetailData data;
    }

    [Serializable]
    public class AlefyTrackDetailData
    {
        public AlefyTrackDto track;
    }

    [Serializable]
    public class AlefyTrackDto
    {
        public int id;
        public string title;
        public string artist;
        public string album;
        public string album_artist;
        public string genre;
        public int year;
        public int track_number;
        public int disc_number;
        public int duration;
        public long file_size;
        public string cover_art_path;
        public int play_count;
        public string last_played_at;
        public string created_at;
    }

    [Serializable]
    public class AlefyErrorResponse
    {
        public bool success;
        public AlefyError error;
    }

    [Serializable]
    public class AlefyError
    {
        public string message;
    }
}
