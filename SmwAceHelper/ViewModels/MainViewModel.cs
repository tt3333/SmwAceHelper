using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Reactive.Bindings;
using SmwAceHelper.Models;
using SmwAceHelper.Properties;
using SmwAceHelper.Utilities;
using SmwAceHelper.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WPFLocalizeExtension.Extensions;

namespace SmwAceHelper.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public ReactivePropertySlim<int> ThumbnailScale { get { return Model.Current.ThumbnailScale; } }
        public ReactivePropertySlim<int> ZoomScale { get { return Model.Current.ZoomScale; } }
        public ObservableCollection<string> RecentFiles { get { return Model.Current.RecentFiles; } }
        public ReactivePropertySlim<Language> Language { get { return Model.Current.Language; } }
        public ReadOnlyReactivePropertySlim<double> ZoomViewWidth { get; }
        public ReadOnlyReactivePropertySlim<double> ZoomViewHeight { get; }
        public ReadOnlyReactivePropertySlim<double> MinWidth { get; }
        public ReadOnlyReactivePropertySlim<string?> Title { get; }
        public ObservableCollection<CommandBinding> CommandBindings { get; }
        public RelayCommand<string> RecentFilesCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand InsertCommand { get; }
        public ReactiveCommand<string> MoveUpCommand { get; }
        public ReactiveCommand<string> MoveDownCommand { get; }
        public ReactiveCommand<string> MoveLeftCommand { get; }
        public ReactiveCommand<string> MoveRightCommand { get; }

        public MainViewModel()
        {
            // subscribe
            Model.Current.ThumbnailScale.Subscribe(_ => UpdateWindowSize());
            Model.Current.ZoomScale.Subscribe(_ => UpdateWindowSize());
            ZoomViewWidth = Model.Current.ZoomScale.CombineLatest(Model.Current.DpiScale, (scale, dpi) => scale * 64 / dpi.DpiScaleX).ToReadOnlyReactivePropertySlim();
            ZoomViewHeight = Model.Current.ZoomScale.CombineLatest(Model.Current.DpiScale, (scale, dpi) => scale * 64 / dpi.DpiScaleY).ToReadOnlyReactivePropertySlim();
            MinWidth = ZoomViewWidth.Select(x => x + 512).ToReadOnlyReactivePropertySlim();
            Title = Model.Current.CurrentProject.CombineLatest(Model.Current.Dirty, Model.Current.Language, (project, dirty, language) => GetTitle(project, dirty)).ToReadOnlyReactivePropertySlim();

            // bind application commands
            CommandBindings = new ObservableCollection<CommandBinding>
            {
                new CommandBinding(ApplicationCommands.New, NewExecuted),
                new CommandBinding(ApplicationCommands.Open, OpenExecuted),
                new CommandBinding(ApplicationCommands.Save, SaveExecuted, SaveCanExecute),
                new CommandBinding(ApplicationCommands.SaveAs, SaveAsExecuted),
                new CommandBinding(ApplicationCommands.Undo, StrategyViewModel.UndoExecuted, (sender, e) => e.CanExecute = Model.Current.History.CanUndo),
                new CommandBinding(ApplicationCommands.Redo, StrategyViewModel.RedoExecuted, (sender, e) => e.CanExecute = Model.Current.History.CanRedo),
            };

            // create commands
            RecentFilesCommand = new RelayCommand<string>(OpenRecentFile);
            ExitCommand = new RelayCommand(() => Application.Current.MainWindow?.Close());
            InsertCommand = new RelayCommand(() => Insert());
            MoveUpCommand = Model.Current.SelectedItemObserver.Select(x => x?.PlayerY > StrategyItem.PLAYER_Y_MIN).ToReactiveCommand<string>().WithSubscribe(x => Move(0, -int.Parse(x)));
            MoveDownCommand = Model.Current.SelectedItemObserver.Select(x => x?.PlayerY < StrategyItem.PLAYER_Y_MAX).ToReactiveCommand<string>().WithSubscribe(x => Move(0, int.Parse(x)));
            MoveLeftCommand = Model.Current.SelectedItemObserver.Select(x => x?.PlayerX > StrategyItem.PLAYER_X_MIN).ToReactiveCommand<string>().WithSubscribe(x => Move(-int.Parse(x), 0));
            MoveRightCommand = Model.Current.SelectedItemObserver.Select(x => x?.PlayerX < StrategyItem.PLAYER_X_MAX).ToReactiveCommand<string>().WithSubscribe(x => Move(int.Parse(x), 0));
        }

        private string GetTitle(string? currentProject, bool dirty)
        {
            StringBuilder sb = new StringBuilder();
            if (dirty)
            {
                sb.Append("* ");
            }
            // If currentProject is null, GetFileName(currentProject) returns null.
            sb.Append(Path.GetFileName(currentProject) ?? LocExtension.GetLocalizedValue<string>(nameof(StringResources.UNTITLED)));
            sb.Append(" - ");
            sb.Append(LocExtension.GetLocalizedValue<string>(nameof(StringResources.TITLE)));
            sb.Append(" v");
            sb.Append(Assembly.GetExecutingAssembly().GetName().Version);
            return sb.ToString();
        }

        private void UpdateWindowSize()
        {
            if (Application.Current.MainWindow is MainView view)
            {
                view.UpdateWindowSize();
            }
        }

        #region commands

        private void NewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (SaveChanges())
            {
                Model.Current.NewProject();
            }
        }

        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (SaveChanges())
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    DefaultExt = ".json",
                    Filter = "JSON (.json)|*.json",
                    Title = LocExtension.GetLocalizedValue<string>(nameof(StringResources.DIALOG_OPEN)),
                };
                if (dialog.ShowDialog() == true)
                {
                    Model.Current.LoadProject(dialog.FileName);
                }
            }
        }

        private void OpenRecentFile(string? path)
        {
            if (path != null)
            {
                if (SaveChanges())
                {
                    Model.Current.LoadProject(path);
                }
            }
        }

        private void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            string? path = Model.Current.CurrentProject.Value;
            if (!string.IsNullOrWhiteSpace(path))
            {
                Model.Current.SaveProject(path);
            }
        }

        private void SaveCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrWhiteSpace(Model.Current.CurrentProject.Value);
        }

        private void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                DefaultExt = ".json",
                Filter = "JSON (.json)|*.json",
                Title = LocExtension.GetLocalizedValue<string>(nameof(StringResources.DIALOG_SAVE_AS)),
            };
            if (dialog.ShowDialog() == true)
            {
                Model.Current.SaveProject(dialog.FileName);
            }
        }

        private void Move(int deltaX, int deltaY)
        {
            StrategyItem? item = Model.Current.SelectedItem.Value;
            if (item != null)
            {
                if (item.PlayerX.HasValue)
                {
                    item.PlayerX = (short)(item.PlayerX.Value + deltaX);
                }
                if (item.PlayerY.HasValue)
                {
                    item.PlayerY = (short)(item.PlayerY.Value + deltaY);
                }
            }
        }

        private void Insert()
        {
            int rowIndex;
            if ((Model.Current.Strategy.Count == 0) || (Model.Current.SelectedItem.Value == null))
            {
                // add to the end
                rowIndex = Model.Current.Strategy.Count;
            }
            else
            {
                // Insert on current row
                rowIndex = Model.Current.Strategy.IndexOf(Model.Current.SelectedItem.Value);
            }
            Model.Current.Strategy.Insert(rowIndex, new StrategyItem());
            WeakReferenceMessenger.Default.Send(new SelectCellRequest(rowIndex, -1));
        }

        #endregion

        /// <summary>
        /// Save changes if necessary
        /// </summary>
        /// <returns>continue/cancel</returns>
        private bool SaveChanges()
        {
            if (!Model.Current.Dirty.Value)
            {
                // no changes
                return true;
            }

            string message = LocExtension.GetLocalizedValue<string>(nameof(StringResources.SAVE_CHANGES));
            string caption = LocExtension.GetLocalizedValue<string>(nameof(StringResources.TITLE));
            MessageBoxResult result = MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveFileDialog dialog = new SaveFileDialog
                    {
                        DefaultExt = ".json",
                        Filter = "JSON (.json)|*.json"
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        if (Model.Current.SaveProject(dialog.FileName))
                        {
                            // saved
                            return true;
                        }
                    }
                    break;

                case MessageBoxResult.No:
                    // discard
                    return true;

                case MessageBoxResult.None:
                case MessageBoxResult.Cancel:
                default:
                    break;
            }

            // cancel
            return false;
        }

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                Model.Current.DpiScale.Value = VisualTreeHelper.GetDpi(window);
                int x = Model.Current.WindowX.Value;
                int y = Model.Current.WindowY.Value;
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                WINDOWPLACEMENT placement = new WINDOWPLACEMENT
                {
                    showCmd = SW.SHOWNORMAL,
                    rcNormalPosition = new RECT(x, y, x + (int)window.ActualWidth, y + (int)window.ActualHeight),
                };
                NativeMethods.SetWindowPlacement(hwnd, placement);
            }
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            if (SaveChanges())
            {
                if (sender is Window window)
                {
                    IntPtr hwnd = new WindowInteropHelper(window).Handle;
                    WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                    if (NativeMethods.GetWindowPlacement(hwnd, placement))
                    {
                        Model.Current.WindowX.Value = placement.rcNormalPosition.left;
                        Model.Current.WindowY.Value = placement.rcNormalPosition.top;
                    }
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        public void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            Model.Current.DpiScale.Value = e.NewDpi;
            UpdateWindowSize();
        }
    }
}
