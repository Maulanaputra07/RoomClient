using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoomClient.Services.Voice
{
    public static class VoiceQueryProcessor
    {
        private static readonly string[] CommandPrefixes =
        [
            "putar lagu",
            "putarkan lagu",
            "mainkan lagu",
            "cari lagu",
            "search lagu",
            "putar",
            "cari",
            "search"
        ];

        public static string Process(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var query = text.Trim();

            foreach (var prefix in CommandPrefixes)
            {
                if (query.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    query = query[prefix.Length..].Trim();
                    break;
                }
            }

            query = Regex.Replace(
                query,
                @"[^\p{L}\p{N}\s]",
                "");

            query = Regex.Replace(
                query,
                @"\s+",
                " ");

            return query.Trim();
        }
    }
}
