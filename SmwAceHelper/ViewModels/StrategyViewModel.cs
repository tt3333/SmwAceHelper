using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Reactive.Bindings;
using SmwAceHelper.Models;
using SmwAceHelper.Utilities;
using SmwAceHelper.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace SmwAceHelper.ViewModels
{
    public class StrategyViewModel : ObservableObject
    {
        private class ParameterInfo
        {
            public PropertyInfo PropertyInfo { get; }
            public bool IgnoreWhenPasteRow { get; }

            public ParameterInfo(PropertyInfo propertyInfo, bool ignoreWhenPasteRow)
            {
                PropertyInfo = propertyInfo;
                IgnoreWhenPasteRow = ignoreWhenPasteRow;
            }
        }

        private static readonly ParameterInfo[] parameterInfo = new ParameterInfo[]
        {
#pragma warning disable CS8604
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.Direction)), false),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.PlayerX)), false),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.PlayerY)), false),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.RideOnYoshi)), false),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.YoshiX)), true),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.YoshiY)), true),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.HaveShell)), false),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.ShellX)), true),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.ShellY)), true),
            new ParameterInfo(typeof(StrategyItem).GetProperty(nameof(StrategyItem.Memo)), false),
#pragma warning restore CS8604
        };

        public ReactivePropertySlim<int> SelectedIndex { get; } = new ReactivePropertySlim<int>();
        public ReactivePropertySlim<int> ColumnIndex { get { return Model.Current.ColumnIndex; } }
        public ReactivePropertySlim<StrategyItem?> SelectedItem { get { return Model.Current.SelectedItem; } }
        public ObservableCollection<StrategyItem> Strategy { get { return Model.Current.Strategy; } }
        public ObservableCollection<CommandBinding> CommandBindings { get; }

        public StrategyViewModel()
        {
            CommandBindings = new ObservableCollection<CommandBinding>
            {
                new CommandBinding(ApplicationCommands.Paste, PasteExecuted, PasteCanExecute),
                new CommandBinding(ApplicationCommands.Delete, DeleteExecuted, DeleteCanExecute),
            };
        }

        #region Undo/Redo command

        public static void UndoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            HistoryItem? historyItem = Model.Current.History.Undo();
            if (historyItem is PropertyChangingHistoryItem propertyChanging)
            {
                StrategyItem? item = Model.Current.Strategy.ElementAtOrDefault(propertyChanging.RowIndex);
                if (item != null)
                {
                    int columnIndex = Array.FindIndex(parameterInfo, x => x.PropertyInfo.Name.Equals(propertyChanging.PropertyName));
                    if (columnIndex >= 0)
                    {
                        Model.Current.History.Logging = false;
                        parameterInfo[columnIndex].PropertyInfo.SetValue(item, propertyChanging.OldValue);
                        Model.Current.History.Logging = true;
                        WeakReferenceMessenger.Default.Send(new SelectCellRequest(propertyChanging.RowIndex, propertyChanging.ColumnIndex));
                    }
                }
            }
            else if (historyItem is CollectionChangedHistoryItem collectionChanged)
            {
                Model.Current.History.Logging = false;
                if (collectionChanged.NewIndex >= 0)
                {
                    Model.Current.Strategy.RemoveAt(collectionChanged.NewIndex);
                    WeakReferenceMessenger.Default.Send(new SelectCellRequest(collectionChanged.NewIndex, -1));
                }
                if ((collectionChanged.OldIndex >= 0) && (collectionChanged.OldItem != null))
                {
                    Model.Current.Strategy.Insert(collectionChanged.OldIndex, collectionChanged.OldItem);
                    WeakReferenceMessenger.Default.Send(new SelectCellRequest(collectionChanged.OldIndex, -1));
                }
                Model.Current.History.Logging = true;
            }
        }

        public static void RedoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            HistoryItem? historyItem = Model.Current.History.Redo();
            if (historyItem is PropertyChangingHistoryItem propertyChanging)
            {
                StrategyItem? item = Model.Current.Strategy.ElementAtOrDefault(propertyChanging.RowIndex);
                if (item != null)
                {
                    int columnIndex = Array.FindIndex(parameterInfo, x => x.PropertyInfo.Name.Equals(propertyChanging.PropertyName));
                    if (columnIndex >= 0)
                    {
                        Model.Current.History.Logging = false;
                        parameterInfo[columnIndex].PropertyInfo.SetValue(item, propertyChanging.NewValue);
                        Model.Current.History.Logging = true;
                        WeakReferenceMessenger.Default.Send(new SelectCellRequest(propertyChanging.RowIndex, propertyChanging.ColumnIndex));
                    }
                }
            }
            else if (historyItem is CollectionChangedHistoryItem collectionChanged)
            {
                Model.Current.History.Logging = false;
                if (collectionChanged.OldIndex >= 0)
                {
                    Model.Current.Strategy.RemoveAt(collectionChanged.OldIndex);
                    WeakReferenceMessenger.Default.Send(new SelectCellRequest(collectionChanged.OldIndex, -1));
                }
                if ((collectionChanged.NewIndex >= 0) && (collectionChanged.NewItem != null))
                {
                    Model.Current.Strategy.Insert(collectionChanged.NewIndex, collectionChanged.NewItem);
                    WeakReferenceMessenger.Default.Send(new SelectCellRequest(collectionChanged.NewIndex, -1));
                }
                Model.Current.History.Logging = true;
            }
        }

        #endregion

        #region Paste command

        private void PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            StrategyItem? item = SelectedItem.Value;
            if (item != null)
            {
                string clipboardText = GetClipboardText();
                if (SelectedIndex.Value < 0)
                {
                    // single cell
                    object? data = GetPasteDataForCell(clipboardText, ColumnIndex.Value);
                    if (data != null)
                    {
                        parameterInfo.ElementAtOrDefault(ColumnIndex.Value)?.PropertyInfo?.SetValue(item, data);
                    }
                }
                else
                {
                    // entire row
                    object?[]? data = GetPasteDataForRow(clipboardText);
                    if (data?.Length == parameterInfo.Length)
                    {
                        for (int columnIndex = 0; columnIndex < parameterInfo.Length; columnIndex++)
                        {
                            if (!parameterInfo[columnIndex].IgnoreWhenPasteRow)
                            {
                                parameterInfo[columnIndex].PropertyInfo.SetValue(item, data[columnIndex]);
                            }
                        }
                    }
                }
            }
        }

        private void PasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedItem.Value != null)
            {
                string clipboardText = GetClipboardText();
                if (SelectedIndex.Value < 0)
                {
                    // single cell
                    object? data = GetPasteDataForCell(clipboardText, ColumnIndex.Value);
                    e.CanExecute = (data != null);
                }
                else
                {
                    // entire row
                    object?[]? data = GetPasteDataForRow(clipboardText);
                    e.CanExecute = (data != null);
                }
            }
        }

        private string GetClipboardText()
        {
            string text = Clipboard.GetText();
            if (text.EndsWith(Environment.NewLine))
            {
                return text.Substring(0, text.Length - Environment.NewLine.Length);
            }
            return text;
        }

        private object?[]? GetPasteDataForRow(string clipboardText)
        {
            string[] texts = clipboardText.Split('\t');
            if (texts.Length != parameterInfo.Length)
            {
                return null;
            }
            object?[] ret = new object[parameterInfo.Length];
            for (int i = 0; i < parameterInfo.Length; i++)
            {
                if (!parameterInfo[i].IgnoreWhenPasteRow)
                {
                    ret[i] = GetPasteDataForCell(texts[i], i);
                    if (ret[i] == DependencyProperty.UnsetValue)
                    {
                        return null;
                    }
                }
            }
            return ret;
        }

        private object? GetPasteDataForCell(string clipboardText, int columnIndex)
        {
            ParameterInfo? info = parameterInfo.ElementAtOrDefault(columnIndex);
            if (info != null)
            {
                if (info.PropertyInfo.PropertyType.Equals(typeof(PlayerDirection)))
                {
                    if (DirectionConverter.ConvertBack(clipboardText) is PlayerDirection direction)
                    {
                        return direction;
                    }
                }
                else if (info.PropertyInfo.PropertyType.Equals(typeof(short?)))
                {
                    object? obj = HexConverter.ConvertBack(clipboardText);
                    if ((obj == null) || (obj is short))
                    {
                        if (SelectedItem.Value?.CanSetValue(info.PropertyInfo.Name, obj) == true)
                        {
                            return obj;
                        }
                    }
                }
                else if (info.PropertyInfo.PropertyType.Equals(typeof(bool)))
                {
                    bool boolData;
                    if (bool.TryParse(clipboardText, out boolData))
                    {
                        return boolData;
                    }
                }
                else if (info.PropertyInfo.PropertyType.Equals(typeof(string)))
                {
                    return clipboardText;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        #endregion

        #region Delete command

        private void DeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            StrategyItem? item = SelectedItem.Value;
            if (item != null)
            {
                int index = SelectedIndex.Value;
                if (index >= 0)
                {
                    // entire row
                    Strategy.RemoveAt(index);
                    SelectedIndex.Value = index;
                }
                else
                {
                    // single cell
                    PropertyInfo? info = parameterInfo.ElementAtOrDefault(ColumnIndex.Value)?.PropertyInfo;
                    if (info != null)
                    {
                        if (info.PropertyType.Equals(typeof(short?)))
                        {
                            info.SetValue(item, null);
                        }
                        else if (info.PropertyType.Equals(typeof(bool)))
                        {
                            info.SetValue(item, false);
                        }
                        else if (info.PropertyType.Equals(typeof(string)))
                        {
                            info.SetValue(item, string.Empty);
                        }
                    }
                }
            }
        }

        private void DeleteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedItem.Value != null)
            {
                if (SelectedIndex.Value >= 0)
                {
                    // entire row
                    e.CanExecute = true;
                }
                else
                {
                    // single cell
                    PropertyInfo? info = parameterInfo.ElementAtOrDefault(ColumnIndex.Value)?.PropertyInfo;
                    if (info != null)
                    {
                        if (info.PropertyType.Equals(typeof(short?)) || info.PropertyType.Equals(typeof(bool)) || info.PropertyType.Equals(typeof(string)))
                        {
                            e.CanExecute = true;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
