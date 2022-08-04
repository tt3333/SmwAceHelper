using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SmwAceHelper.Models
{
    public class HistoryItem
    {
        public int ColumnIndex { get; init; }
    }

    public class PropertyChangingHistoryItem : HistoryItem
    {
        public string? PropertyName { get; init; }
        public object? OldValue { get; init; }
        public object? NewValue { get; init; }
        public int RowIndex { get; init; }
    }

    public class CollectionChangedHistoryItem : HistoryItem
    {
        public StrategyItem? OldItem { get; init; }
        public StrategyItem? NewItem { get; init; }
        public int OldIndex { get; init; }
        public int NewIndex { get; init; }
    }

    public class History
    {
        private List<HistoryItem> history = new List<HistoryItem>();
        private int currentIndex;

        public bool CanUndo { get { return currentIndex > 0; } }
        public bool CanRedo { get { return currentIndex < history.Count; } }
        public bool Logging { get; set; } = true;

        public void OnPropertyChanging(StrategyItem item, object? oldValue, object? newValue, [CallerMemberName] string? propertyName = null)
        {
            if (Logging)
            {
                PropertyChangingHistoryItem historyItem = new PropertyChangingHistoryItem
                {
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    RowIndex = Model.Current.Strategy.IndexOf(item),
                    ColumnIndex = Model.Current.ColumnIndex.Value,
                };
                Add(historyItem);
            }
        }

        public void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (Logging)
            {
                CollectionChangedHistoryItem historyItem = new CollectionChangedHistoryItem
                {
                    OldItem = e.OldItems?.OfType<StrategyItem>().FirstOrDefault(),
                    NewItem = e.NewItems?.OfType<StrategyItem>().FirstOrDefault(),
                    OldIndex = e.OldStartingIndex,
                    NewIndex = e.NewStartingIndex,
                    ColumnIndex = Model.Current.ColumnIndex.Value,
                };
                Add(historyItem);
            }
        }

        private void Add(HistoryItem item)
        {
            if (currentIndex < history.Count)
            {
                history.RemoveRange(currentIndex, history.Count - currentIndex);
            }
            history.Add(item);
            currentIndex++;
        }

        public void Clear()
        {
            history.Clear();
            currentIndex = 0;
        }

        public HistoryItem? Undo()
        {
            if (currentIndex > 0)
            {
                return history[--currentIndex];
            }
            return null;
        }

        public HistoryItem? Redo()
        {
            if (currentIndex < history.Count)
            {
                return history[currentIndex++];
            }
            return null;
        }
    }
}
