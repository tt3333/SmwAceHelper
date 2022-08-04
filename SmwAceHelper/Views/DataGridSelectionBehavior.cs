using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Xaml.Behaviors;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmwAceHelper.Views
{
    public class SelectCellRequest
    {
        public int RowIndex { get; }
        public int ColumnIndex { get; }

        public SelectCellRequest(int rowIndex, int columnIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }

    public class DataGridSelectionBehavior : Behavior<DataGrid>
    {
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DataGridSelectionBehavior));
        public static readonly DependencyProperty ColumnIndexProperty = DependencyProperty.Register(nameof(ColumnIndex), typeof(int), typeof(DataGridSelectionBehavior));
        public static readonly DependencyProperty NextElementProperty = DependencyProperty.Register(nameof(NextElement), typeof(FrameworkElement), typeof(DataGridSelectionBehavior));

        private IInputElement? focusElement;

        /// <summary>
        /// Item in the current selection
        /// </summary>
        /// <remarks>If no entire rows are selected, DataGrid.SelectedItem property is null.</remarks>
        public object? SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        /// <summary>
        /// Column in the current selection
        /// </summary>
        public int ColumnIndex
        {
            get { return (int)GetValue(ColumnIndexProperty); }
            set { SetValue(ColumnIndexProperty, value); }
        }

        /// <summary>
        /// Control to move focus with Tab key
        /// </summary>
        public FrameworkElement? NextElement
        {
            get { return (FrameworkElement?)GetValue(NextElementProperty); }
            set { SetValue(NextElementProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GotKeyboardFocus += AssociatedObject_GotKeyboardFocus;
            AssociatedObject.SelectedCellsChanged += AssociatedObject_SelectedCellsChanged;
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            WeakReferenceMessenger.Default.Register<SelectCellRequest>(this, OnSelectCellRequest);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.GotKeyboardFocus -= AssociatedObject_GotKeyboardFocus;
            AssociatedObject.SelectedCellsChanged -= AssociatedObject_SelectedCellsChanged;
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            WeakReferenceMessenger.Default.Unregister<SelectCellRequest>(this);
        }

        private void OnSelectCellRequest(object recipient, SelectCellRequest message)
        {
            DataGridColumn? column = AssociatedObject.Columns.ElementAtOrDefault(message.ColumnIndex);
            SelectCell(message.RowIndex, column);
        }

        private void SelectCell(int rowIndex, DataGridColumn? column)
        {
            if ((0 <= rowIndex) && (rowIndex < AssociatedObject.Items.Count))
            {
                object? item = AssociatedObject.Items[rowIndex];
                SelectCell(item, column);
            }
            else
            {
                SelectCell(null, column);
            }
        }

        private void SelectCell(object? item, DataGridColumn? column)
        {
            // unselect
            AssociatedObject.SelectedItem = null;
            AssociatedObject.UnselectAllCells();

            if (item != null)
            {
                // If Focus() is called after selecting a cell, when the entire row is selected and then the right key is pressed twice, the focus moves to the wrong cell.
                // Therefore, Focus() must be called before selecting a cell.
                AssociatedObject.Focus();

                if (column == null)
                {
                    // select entire row
                    AssociatedObject.SelectedItem = item;
                }
                else
                {
                    // select single cell
                    DataGridCellInfo cellInfo = new DataGridCellInfo(item, column);
                    AssociatedObject.SelectedCells.Add(cellInfo);
                    AssociatedObject.CurrentCell = cellInfo;
                }
                AssociatedObject.ScrollIntoView(item);
            }
        }

        private void AssociatedObject_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if ((focusElement != null) && !focusElement.Equals(e.OldFocus))
            {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (AssociatedObject.SelectedItem != null)
                        {
                            // select entire row
                            SelectCell(AssociatedObject.SelectedItem, null);
                        }
                        else if (AssociatedObject.SelectedCells.Any())
                        {
                            // select single cell
                            DataGridCellInfo cell = AssociatedObject.SelectedCells[0];
                            SelectCell(cell.Item, cell.Column);
                        }
                    }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    e.Handled = true;
            }
            focusElement = e.NewFocus;
        }

        private void AssociatedObject_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            SelectedItem = AssociatedObject.SelectedCells.Where(x => x.IsValid).Select(x => x.Item).FirstOrDefault();
            ColumnIndex = AssociatedObject.Columns.IndexOf(AssociatedObject.CurrentColumn);
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"PreviewKeyDown({sender.GetType().Name}, {e.Source.GetType().Name}, {e.OriginalSource.GetType().Name})");
            bool editing = false;
            switch (e.OriginalSource)
            {
                case TextBox textBox:
                    editing = TextBox_PreviewKeyDown(textBox, e);
                    break;
                case CheckBox checkBox:
                    editing = CheckBox_PreviewKeyDown(checkBox, e);
                    break;
                case ComboBox comboBox:
                    editing = ComboBox_PreviewKeyDown(comboBox, e);
                    break;
                case ComboBoxItem comboBoxItem:
                    editing = ComboBox_PreviewKeyDown(comboBoxItem.Parent as ComboBox, e);
                    break;
                case DataGridCell cell:
                    editing = ComboBox_PreviewKeyDown(FindChild<ComboBox>(cell), e);
                    break;
                case DataGrid:
                    editing = DataGrid_PreviewKeyDown(e);
                    break;
            }

            switch (e.Key)
            {
                case Key.Left:
                    if (!editing && (AssociatedObject.SelectedIndex < 0) && (AssociatedObject.CurrentColumn?.DisplayIndex == 0))
                    {
                        // select entire row
                        SelectCell(AssociatedObject.CurrentCell.Item, null);
                        e.Handled = true;
                    }
                    break;

                case Key.Tab:
                    if (!editing)
                    {
                        if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
                        {
                            AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                        }
                        else if (NextElement != null)
                        {
                            NextElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }
                        e.Handled = true;
                    }
                    break;
            }
        }

        private bool CheckBox_PreviewKeyDown(CheckBox checkBox, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Tab:
                    AssociatedObject.CommitEdit();
                    break;
            }
            return false;
        }

        private bool ComboBox_PreviewKeyDown(ComboBox? comboBox, KeyEventArgs e)
        {
            if (comboBox != null)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        // Use the spacebar to open and close the drop-down.
                        comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                        break;

                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                        if (!comboBox.IsDropDownOpen)
                        {
                            // If drop-down is closed, use the cursor keys to move the cell.
                            FindAncestor<DataGridCell>(comboBox)?.Focus();
                        }
                        break;

                    case Key.Tab:
                        if (comboBox.IsDropDownOpen)
                        {
                            // Suppress focus moving to the next ComboBoxItem.
                            e.Handled = true;
                        }
                        break;
                }
                return comboBox.IsDropDownOpen;
            }
            return false;
        }

        private bool TextBox_PreviewKeyDown(TextBox textBox, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    if (!e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Suppress focus moving to the next line.
                        textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                        AssociatedObject.CommitEdit();
                    }
                    else if (textBox.AcceptsReturn)
                    {
                        // Inserting a line break.
                        int index = textBox.CaretIndex;
                        textBox.Text = textBox.Text.Insert(index, Environment.NewLine);
                        textBox.CaretIndex = index + 1;
                    }
                    e.Handled = true;
                    break;

                case Key.Tab:
                    if (!textBox.AcceptsTab)
                    {
                        // Suppress focus moving to the next cell.
                        e.Handled = true;
                    }
                    break;
            }
            return true;
        }

        private bool DataGrid_PreviewKeyDown(KeyEventArgs e)
        {
            int index = AssociatedObject.SelectedIndex;
            if (index >= 0)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        if (index > 0)
                        {
                            SelectCell(index - 1, null);
                        }
                        e.Handled = true;
                        break;

                    case Key.Down:
                        if (index < AssociatedObject.Items.Count - 1)
                        {
                            SelectCell(index + 1, null);
                        }
                        e.Handled = true;
                        break;

                    case Key.Left:
                        e.Handled = true;
                        break;

                    case Key.Right:
                        // Select the first column cell.
                        SelectCell(AssociatedObject.SelectedItem, AssociatedObject.ColumnFromDisplayIndex(0));
                        e.Handled = true;
                        break;
                }
            }
            return false;
        }

        private static T? FindAncestor<T>(DependencyObject obj) where T : class
        {
            while (obj != null)
            {
                obj = VisualTreeHelper.GetParent(obj);
                if (obj is T ret)
                {
                    return ret;
                }
            }
            return null;
        }

        private static T? FindChild<T>(DependencyObject obj) where T : class
        {
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T ret)
                {
                    return ret;
                }
                else if (child != null)
                {
                    T? ret2 = FindChild<T>(child);
                    if (ret2 != null)
                    {
                        return ret2;
                    }
                }
            }
            return null;
        }
    }
}
