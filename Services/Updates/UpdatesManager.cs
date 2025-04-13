using Common.Logger;
using OutfitTool.Services.Settings;
using OutfitTool.View;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Windows.Threading;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;

namespace OutfitTool.Services.Updates
{
    class UpdatesManager
    {
        public delegate void OnUpdateManagerStatusChanged(bool Busy, RepositoryCollection? repositoryItems);

        public bool Busy = false;

        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cancellationTokenSource = null;
        public RepositoryCollection repositoryItems = new RepositoryCollection();
        LoggerInterface logger;
        SettingsManager<AppSettings> settingsManager;
        public event EventHandler? StatusChanged;
        private readonly Dispatcher _dispatcher;
        public OnUpdateManagerStatusChanged? updateManagerStatusChanged = null;

        private string UpdatesStatus { set{
                if (_dispatcher != null){
                    _dispatcher.Invoke(() => { logger.Status(value); });
                }
            }
        }

        public UpdatesManager()
        {
            logger = ServiceLocator.GetService<LoggerInterface>();
            settingsManager = ServiceLocator.GetService<SettingsManager<AppSettings>>();
            _httpClient = new HttpClient();
            UpdatesStatus = "Обновления не проверялись";
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void LoadList()
        {
            this.Busy = true;
            this.updateManagerStatusChanged(Busy, null);

            UpdatesStatus = "Чтение списка обновлений...";
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CheckForUpdates(_cancellationTokenSource.Token));
        }

        private async Task CheckForUpdates(CancellationToken cancellationToken)
        {
            try
            {
                var settings = settingsManager.LoadSettings();
                var response = await _httpClient.GetAsync(settings.updatesRepository, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                repositoryItems = ParseUpdateList(content);

                UpdatesStatus = "Прочитан список модулей из репозитория";

                if (this.updateManagerStatusChanged != null && _dispatcher != null)
                {
                    _dispatcher.Invoke(() => {
                        this.Busy = false;
                        this.updateManagerStatusChanged(Busy, repositoryItems); 
                    });
                }
            }
            catch (OperationCanceledException)
            {
                this.Busy = false;
                UpdatesStatus = "Проверка обновлений отменена";
                this.updateManagerStatusChanged(Busy, null);
            }
            catch (Exception ex)
            {
                this.Busy = false;
                UpdatesStatus = $"Ошибка при проверке обновлений: {ex.Message}";
                this.updateManagerStatusChanged(Busy, null);
            }
        }

        public void CancelCheck()
        {
            _cancellationTokenSource?.Cancel();
            UpdatesStatus = "Проверка обновлений отменена пользователем";
            Busy = false;
            this.updateManagerStatusChanged(Busy, null);
        }

        private RepositoryCollection ParseUpdateList(string yamlContent)
        {
            var deserializer = new DeserializerBuilder().Build();
            Dictionary<object, object> result = deserializer.Deserialize<dynamic>(yamlContent);
            Dictionary<object, object> modules = result as Dictionary<object, object>;
            var RepositoryItems = new RepositoryCollection();

            foreach (KeyValuePair<object, object> entry in modules)
            {
                var versionList = entry.Value as Dictionary<object, object>;
                foreach(KeyValuePair<object, object> versionEntry in versionList)
                {
                    string? version = versionEntry.Key.ToString();
                    var parametersList = versionEntry.Value as Dictionary<object, object>;
                    if (version == null || parametersList == null)
                    {
                        continue;
                    }

                    RepositoryItem item = new RepositoryItem();
                    item.Version = Version.FromString(version);
                    string? name = entry.Key.ToString();
                    if (name == null)
                    {
                        continue;
                    }
                    item.Name = name;
                    string? changes = null;
                    string? require = null;
                    foreach (var parametersEntry in parametersList)
                    {
                        if (parametersEntry.Key.ToString() == "changes" && parametersEntry.Value?.ToString() != null)
                        {
                            changes = parametersEntry.Value?.ToString();
                        }
                        if (parametersEntry.Key.ToString() == "require" && parametersEntry.Value?.ToString() != null)
                        {
                            require = parametersEntry.Value?.ToString();
                        }
                    }
                    if (changes == null)
                    {
                        continue;
                    }
                    if (require == null)
                    {
                        require = "3.0";
                    }
                    item.Require = Version.FromString(require);
                    RepositoryItems.Add(item);
                }
            }
            return RepositoryItems;
        }

        private string trimSlashes(string url)
        {
            if (url.EndsWith('/'))
            {
                return trimSlashes(url.Substring(0, url.Length - 1));
            }
            return url;
        }
    }
}
