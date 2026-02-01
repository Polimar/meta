#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BeatSaberAlefy.UI
{
    public static class DebugLog
    {
        const string LogPath = "c:\\Users\\Valerio\\meta\\.cursor\\debug.log";
        static readonly object _lock = new object();

        public static void Write(string location, string message, string hypothesisId, params (string k, object v)[] data)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\"timestamp\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                sb.Append(",\"location\":\"").Append(Escape(location)).Append("\"");
                sb.Append(",\"message\":\"").Append(Escape(message)).Append("\"");
                sb.Append(",\"hypothesisId\":\"").Append(Escape(hypothesisId ?? "")).Append("\"");
                sb.Append(",\"sessionId\":\"debug-session\"");
                if (data != null && data.Length > 0)
                {
                    sb.Append(",\"data\":{");
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append("\"").Append(Escape(data[i].k)).Append("\":\"").Append(Escape(data[i].v?.ToString() ?? "null")).Append("\"");
                    }
                    sb.Append("}");
                }
                sb.Append("}\n");
                lock (_lock) { File.AppendAllText(LogPath, sb.ToString()); }
            }
            catch { }
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }
}
#endif
