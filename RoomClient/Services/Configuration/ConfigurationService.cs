using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Services.Configuration
{
    internal class ConfigurationService : IConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoomClient",
            "config.json"
        );

        public AppConfig Config { get; private set; }
        public ConfigurationService() {
            Config = LoadCreate();
        }

        public AppConfig LoadCreate()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config is not null)
                    {
                        return config;
                    }
                }
                catch { }
            }

            var newConfig = new AppConfig
            {
                DeviceId = Guid.NewGuid().ToString(),
                IsRegistered = false
            };

            Save(newConfig);
            return newConfig;
        }

        public void Save(AppConfig config)
        {
            var directory = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Config = config;
        }
    }
}
