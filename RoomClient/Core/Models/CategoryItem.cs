using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class CategoryItem
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";        // TAMBAHAN — dipakai untuk endpoint
        public string SearchKeyword { get; set; } = ""; // bisa disimpan sbg fallback, atau dihapus kalau tidak dipakai lagi
        public string Icon { get; set; } = "";
    }
}
