using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Config
{
    public static class FeatureFlags
    {
#if VOICE_SEARCH
        public const bool VoiceSearchEnabled = true;
#else
        public const bool VoiceSearchEnabled = false;
#endif
    }
}
