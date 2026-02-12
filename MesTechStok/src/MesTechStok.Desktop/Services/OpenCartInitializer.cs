using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MesTechStok.Core.Integrations.OpenCart;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Services
{
    public interface IOpenCartInitializer
    {
        void Initialize();
    }

    public class OpenCartInitializer : IOpenCartInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OpenCartSettingsOptions _settings;

        public OpenCartInitializer(IServiceProvider serviceProvider, IOptions<OpenCartSettingsOptions> settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        public void Initialize()
        {
            try
            {
                var client = _serviceProvider.GetService<IOpenCartClient>();
                if (client == null) return;

                // Konfigürasyonla bağlantı kurmayı dene (async fire-and-forget)
                _ = client.ConnectAsync(_settings.ApiUrl, _settings.ApiKey);
                GlobalLogger.Instance.LogInfo($"OpenCart init scheduled. Url={_settings.ApiUrl}, AutoSync={_settings.AutoSyncEnabled}", "OpenCartInit");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"OpenCart init error: {ex.Message}", "OpenCartInit");
            }
        }
    }
}


