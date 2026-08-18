using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IYoutubeService
    {
        Task<List<Song>> SearchAsync(string keyword);
        Task<List<Song>> GetByCategoryAsync(string categorySlug);
        Task<string?> GetStreamUrlAsync(string videoId);

        string BuildPlayerHtml(string streamUrl);
    }
}
