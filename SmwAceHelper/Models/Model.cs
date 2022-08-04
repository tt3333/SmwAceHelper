using Microsoft.Toolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using WPFLocalizeExtension.Engine;

namespace SmwAceHelper.Models
{
    public class Model : ObservableObject
    {
        private static readonly string settingsFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmwAceHelper");
        private static readonly string settingsFilePath = Path.Combine(settingsFileDir, "settings.json");
        private static readonly int RECENT_FILES_MAX = 10;

        public static readonly CultureInfo[] Languages = new CultureInfo[]
        {
            CultureInfo.GetCultureInfo("en"),
            CultureInfo.GetCultureInfo("ja"),
        };

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };

        private Settings settings;
        private bool initialized;

        public static Model Current { get; } = new Model();
        public History History { get; } = new History();
        public ReactivePropertySlim<DpiScale> DpiScale { get; } = new ReactivePropertySlim<DpiScale>(new DpiScale(1.0, 1.0));
        public ReactivePropertySlim<string?> CurrentProject { get; } = new ReactivePropertySlim<string?>();
        public ReactivePropertySlim<bool> Dirty { get; } = new ReactivePropertySlim<bool>();
        public ObservableCollection<StrategyItem> Strategy { get; } = new ObservableCollection<StrategyItem>();
        public ReactivePropertySlim<StrategyItem?> SelectedItem { get; } = new ReactivePropertySlim<StrategyItem?>();
        public IObservable<StrategyItem?> SelectedItemObserver { get; }
        public ReactivePropertySlim<int> ColumnIndex { get; } = new ReactivePropertySlim<int>();
        public ObservableCollection<string> RecentFiles { get { return settings.RecentFiles; } }
        public ReactivePropertySlim<Language> Language { get; }
        public ReactivePropertySlim<int> ThumbnailScale { get; }
        public ReactivePropertySlim<int> ZoomScale { get; }
        public ReactivePropertySlim<int> WindowX { get; }
        public ReactivePropertySlim<int> WindowY { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        private Model()
        {
            // create setting properties
            settings = LoadSettings() ?? new Settings();
            Language = settings.ToReactivePropertySlimAsSynchronized(x => x.Language);
            ThumbnailScale = settings.ToReactivePropertySlimAsSynchronized(x => x.ThumbnailScale);
            ZoomScale = settings.ToReactivePropertySlimAsSynchronized(x => x.ZoomScale);
            WindowX = settings.ToReactivePropertySlimAsSynchronized(x => x.WindowX);
            WindowY = settings.ToReactivePropertySlimAsSynchronized(x => x.WindowY);

            // subscribe setting properties
            Language.Subscribe(x =>
            {
                LocalizeDictionary.Instance.Culture = Languages.ElementAtOrDefault((int)x) ?? Languages[0];
                SaveSettings();
            });
            ThumbnailScale.Subscribe(_ => SaveSettings());
            ZoomScale.Subscribe(_ => SaveSettings());
            WindowX.Subscribe(_ => SaveSettings());
            WindowY.Subscribe(_ => SaveSettings());

            // subscribe project data
            Strategy.ObserveElementPropertyChanged().Subscribe(_ => Dirty.Value = true);
            Strategy.CollectionChanged += (sender, e) => Dirty.Value = true;
            Strategy.CollectionChanged += History.OnCollectionChanged;

            // create observer to notify selected item
            SelectedItemObserver = Strategy
                .ObserveElementPropertyChanged()
                .Where(x => object.ReferenceEquals(x.Sender, SelectedItem.Value))
                .Select(x => x.Sender)
                .Merge(SelectedItem);

            // set initialized flag
            initialized = true;
        }

        /// <summary>
        /// Load settings
        /// </summary>
        /// <returns>Settings</returns>
        private Settings? LoadSettings()
        {
            Settings? settings = null;
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath, Encoding.UTF8);
                    settings = JsonSerializer.Deserialize<Settings>(json, options);
                    if (settings != null)
                    {
                        CheckRecentFiles(settings.RecentFiles);
                    }
                }
            }
            catch
            {
            }
            return settings;
        }

        /// <summary>
        /// Save settings
        /// </summary>
        private void SaveSettings()
        {
            if (initialized)
            {
                Task.Run(() =>
                {
                    try
                    {
                        string json = JsonSerializer.Serialize<Settings>(settings, options);
                        Directory.CreateDirectory(settingsFileDir);
                        File.WriteAllText(settingsFilePath, json, Encoding.UTF8);
                    }
                    catch
                    {
                    }
                });
            }
        }

        /// <summary>
        /// Check for the existence and duplication of recently used files.
        /// </summary>
        /// <param name="recentFiles">Recently used files</param>
        private void CheckRecentFiles(IList<string> recentFiles)
        {
            for (int i = recentFiles.Count - 1; i > 0; i--)
            {
                try
                {
                    int j;
                    for (j = 0; j < i; j++)
                    {
                        if (recentFiles[i].Equals(recentFiles[j]))
                        {
                            break;
                        }
                    }
                    if ((j < i) || !File.Exists(recentFiles[i]))
                    {
                        recentFiles.RemoveAt(i);
                    }
                }
                catch
                {
                }
            }
            for (int i = recentFiles.Count - 1; i >= RECENT_FILES_MAX; i--)
            {
                recentFiles.RemoveAt(i);
            }
        }

        /// <summary>
        /// New project
        /// </summary>
        public void NewProject()
        {
            Strategy.Clear();
            History.Clear();
            CurrentProject.Value = null;
            Dirty.Value = false;
        }

        /// <summary>
        /// Load project
        /// </summary>
        /// <param name="path">Project file path</param>
        /// <returns>success/failure</returns>
        public bool LoadProject(string path)
        {
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                StrategyItem[]? items = JsonSerializer.Deserialize<StrategyItem[]>(json, options);
                if (items != null)
                {
                    Strategy.Clear();
                    foreach (StrategyItem item in items)
                    {
                        Strategy.Add(item);
                    }

                    History.Clear();
                    CurrentProject.Value = path;
                    Dirty.Value = false;

                    settings.RecentFiles.Insert(0, path);
                    CheckRecentFiles(settings.RecentFiles);
                    SaveSettings();

                    return true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        /// <summary>
        /// Save project
        /// </summary>
        /// <param name="path">Project file path</param>
        /// <returns>success/failure</returns>
        public bool SaveProject(string path)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    string json = JsonSerializer.Serialize<StrategyItem[]>(Strategy.ToArray(), options);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(path, json, Encoding.UTF8);

                    CurrentProject.Value = path;
                    Dirty.Value = false;

                    settings.RecentFiles.Insert(0, path);
                    CheckRecentFiles(settings.RecentFiles);
                    SaveSettings();

                    return true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
    }
}
