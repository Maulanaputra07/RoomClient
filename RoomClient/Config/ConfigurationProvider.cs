using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Config
{
    public static class ConfigurationProvider
    {
        public static IConfiguration Configuration { get; }

        public static ApiSettings ApiSettings { get; }

        static ConfigurationProvider()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true)
                .Build();

            ApiSettings = new ApiSettings();
            Configuration.GetSection("ApiSettings").Bind(ApiSettings);
        }
    }
}
