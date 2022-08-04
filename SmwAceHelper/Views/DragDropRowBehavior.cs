using Microsoft.Xaml.Behaviors;
using SmwAceHelper.Models;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmwAceHelper.Views
{
    public class DragDropRowBehavior : DragDropRowBehavior<StrategyItem>
    {
    }

    public class DragDropRowBehavior<T> : Behavior<DataGrid>
    {
        private enum IndicatorPosition
        {
            Top,
            Bottom,
        }

        private class IndicatorAdorner : Adorner
        {
            private Pen pen;
            private IndicatorPosition position;

            public IndicatorAdorner(UIElement adornedElement, Pen pen, IndicatorPosition position) : base(adornedElement)
            {
                IsHitTestVisible = false;
                this.pen = pen;
                this.position = position;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                Size size = AdornedElement.DesiredSize;
                switch (position)
                {
                    case IndicatorPosition.Top:
                        drawingContext.DrawLine(pen, new Point(0, 0), new Point(size.Width, 0));
                        break;
                    case IndicatorPosition.Bottom:
                        drawingContext.DrawLine(pen, new Point(0, size.Height), new Point(size.Width, size.Height));
                        break;
                    default:
                        break;
                }
            }
        }

        private AdornerLayer? layer;
        private IndicatorAdorner? adorner;
        private int dragIndex = -1;
        private int dropIndex = -1;

        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(nameof(Brush), typeof(Brush), typeof(DragDropRowBehavior<T>));
        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(DragDropRowBehavior<T>));

        /// <summary>
        /// Brush to draw a line indicating copy/move destination
        /// </summary>
        public Brush Brush
        {
            get { return (Brush)GetValue(BrushProperty); }
            set { SetValue(BrushProperty, value); }
        }

        /// <summary>
        /// Line thickness to indicate copy/move destination
        /// </summary>
        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.DragLeave += AssociatedObject_DragLeave;
            AssociatedObject.DragOver += AssociatedObject_DragOver;
            AssociatedObject.Drop += AssociatedObject_Drop;
            AssociatedObject.AllowDrop = true;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.DragLeave -= AssociatedObject_DragLeave;
            AssociatedObject.DragOver -= AssociatedObject_DragOver;
            AssociatedObject.Drop -= AssociatedObject_Drop;
            AssociatedObject.AllowDrop = false;
        }

        private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.OriginalSource is DataGridHeaderBorder)
            if (e.OriginalSource.GetType().Name.Equals("DataGridHeaderBorder"))
            {
                dragIndex = GetDragIndex(e);
                if (dragIndex >= 0)
                {
                    // select entire row
                    AssociatedObject.UnselectAllCells();
                    AssociatedObject.SelectedIndex = dragIndex;
                    AssociatedObject.Focus();
                    e.Handled = true;

                    // DoDragDrop() is called after Focus() is actually reflected.
                    Dispatcher.InvokeAsync(() =>
                    {
                        DragDropEffects effects = (AssociatedObject.SelectedItem is ICloneable) ? (DragDropEffects.Copy | DragDropEffects.Move) : DragDropEffects.Move;
                        DragDrop.DoDragDrop(AssociatedObject, AssociatedObject.SelectedItem, effects);
                        dragIndex = -1;
                        UpdateIndicator(-1);
                    }, DispatcherPriority.ApplicationIdle);
                }
            }
        }

        private void AssociatedObject_DragLeave(object sender, DragEventArgs e)
        {
            UpdateIndicator(-1);
            e.Handled = true;
        }

        private void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            int index = GetDropIndex(e);
            UpdateIndicator(index);
            e.Handled = true;
        }

        private void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            int index = GetDropIndex(e);
            if ((0 <= index) && (AssociatedObject.ItemsSource is IList list))
            {
                if (e.Effects == DragDropEffects.Copy)
                {
                    // copy
                    if (list[dragIndex] is ICloneable item)
                    {
                        if (index >= dragIndex)
                        {
                            index++;
                        }
                        list.Insert(index, item.Clone());
                        AssociatedObject.SelectedIndex = index;
                    }
                }
                else if (index != dragIndex)
                {
                    // move
                    if (list is ObservableCollection<T> collection)
                    {
                        collection.Move(dragIndex, index);
                    }
                    else
                    {
                        object? item = list[dragIndex];
                        list.RemoveAt(dragIndex);
                        list.Insert(index, item);
                    }
                    AssociatedObject.SelectedIndex = index;
                }
            }
            e.Handled = true;
        }

        private int GetDragIndex(MouseEventArgs e)
        {
            if (AssociatedObject.ItemsSource is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    DataGridRow? row = GetRowItem(i);
                    if (row != null)
                    {
                        Rect rect = VisualTreeHelper.GetDescendantBounds(row);
                        Point point = e.GetPosition(row);
                        if (rect.Contains(point))
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        private int GetDropIndex(DragEventArgs e)
        {
            if ((AssociatedObject.ItemsSource is IList list) && (e.OriginalSource is FrameworkElement element))
            {
                int index = element.DataContext.GetType().Name.Equals("NamedObject") ? list.Count - 1 : list.IndexOf(element.DataContext);
                if (index >= 0)
                {
                    e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Copy) && e.KeyStates.HasFlag(DragDropKeyStates.ControlKey) ? DragDropEffects.Copy : DragDropEffects.Move;
                    return index;
                }
            }
            e.Effects = DragDropEffects.None;
            return -1;
        }

        private DataGridRow? GetRowItem(int index)
        {
            if (AssociatedObject.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                return AssociatedObject.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
            }
            return null;
        }

        private void UpdateIndicator(int dropIndex)
        {
            if (this.dropIndex != dropIndex)
            {
                this.dropIndex = dropIndex;
                if ((layer != null) && (adorner != null))
                {
                    layer.Remove(adorner);
                    layer = null;
                    adorner = null;
                }
                if ((0 <= dropIndex) && (dropIndex != dragIndex))
                {
                    DataGridRow? row = GetRowItem(dropIndex);
                    if (row != null)
                    {
                        layer = AdornerLayer.GetAdornerLayer(row);
                        adorner = new IndicatorAdorner(row, new Pen(Brush, Thickness), dropIndex < dragIndex ? IndicatorPosition.Top : IndicatorPosition.Bottom);
                        layer.Add(adorner);
                    }
                }
            }
        }
    }
}
