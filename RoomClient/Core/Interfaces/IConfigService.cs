using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IConfigService
    {
        AppConfig LoadCreate();
        void Save(AppConfig config);
    }
}
