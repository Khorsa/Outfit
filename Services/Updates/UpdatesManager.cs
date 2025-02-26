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
        public delegate void OnUpdatesListChanged(List<RepositoryItem> repositoryItems);

        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cancellationTokenSource = null;
        public List<RepositoryItem> RepositoryItems = new List<RepositoryItem>();
        LoggerInterface logger;
        SettingsManager<AppSettings> settingsManager;
        public event EventHandler? StatusChanged;
        private readonly Dispatcher _dispatcher;
        private OnUpdatesListChanged? updatesListChangedEvent = null;

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

        public void LoadList(OnUpdatesListChanged? eventOnChangedList = null)
        {
            this.updatesListChangedEvent = eventOnChangedList;
            UpdatesStatus = "Чтение списка обновлений...";
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => CheckForUpdates(_cancellationTokenSource.Token));
        }

        private async Task CheckForUpdates(CancellationToken cancellationToken)
        {
            try
            {
                var settings = settingsManager.LoadSettings();
                string moduleListUrl = trimSlashes(settings.updatesRepository) + "/list.php";
                var response = await _httpClient.GetAsync(moduleListUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                RepositoryItems = ParseUpdateList(content);
                UpdatesStatus = "Прочитан список модулей из репозитория";

                if (this.updatesListChangedEvent != null && _dispatcher != null)
                {
                    _dispatcher.Invoke(() => { this.updatesListChangedEvent(RepositoryItems); });
                }
            }
            catch (OperationCanceledException)
            {
                UpdatesStatus = "Проверка обновлений отменена";
            }
            catch (Exception ex)
            {
                UpdatesStatus = $"Ошибка при проверке обновлений: {ex.Message}";
            }
        }

        public void CancelCheck()
        {
            _cancellationTokenSource?.Cancel();
            UpdatesStatus = "Проверка обновлений отменена пользователем";
        }

        private List<RepositoryItem> ParseUpdateList(string yamlContent)
        {
            var deserializer = new DeserializerBuilder().Build();
            Dictionary<object, object> result = deserializer.Deserialize<dynamic>(yamlContent);
            Dictionary<object, object> modules = result["Modules"] as Dictionary<object, object>;
            var RepositoryItems = new List<RepositoryItem>();

            foreach (KeyValuePair<object, object> entry in modules)
            {
                RepositoryItem item = new RepositoryItem();
                item.Name = entry.Key.ToString();
                string version = entry.Value.ToString();
                item.MajorVersion = int.Parse(version[(version.IndexOf('.') + 1)..]);
                item.MinorVersion = int.Parse(version[..version.IndexOf(".")]);
                RepositoryItems.Add(item);
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
