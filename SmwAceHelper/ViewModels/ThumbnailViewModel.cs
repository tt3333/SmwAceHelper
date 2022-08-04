using Microsoft.Toolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using SmwAceHelper.Models;
using System;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SmwAceHelper.ViewModels
{
    public class ThumbnailViewModel : ObservableObject
    {
        public ReactivePropertySlim<BitmapImage?> SpriteImage { get; } = new ReactivePropertySlim<BitmapImage?>();
        public ReactivePropertySlim<double> SpriteX { get; } = new ReactivePropertySlim<double>();
        public ReactivePropertySlim<double> SpriteY { get; } = new ReactivePropertySlim<double>();
        public ReadOnlyReactivePropertySlim<double> ScaleX { get; }
        public ReadOnlyReactivePropertySlim<double> ScaleY { get; }

        public ThumbnailViewModel()
        {
            ScaleX = Model.Current.ThumbnailScale.CombineLatest(Model.Current.DpiScale, (scale, dpi) => Math.Min(Math.Max(scale, 50), 100) / (dpi.DpiScaleX * 100)).ToReadOnlyReactivePropertySlim();
            ScaleY = Model.Current.ThumbnailScale.CombineLatest(Model.Current.DpiScale, (scale, dpi) => Math.Min(Math.Max(scale, 50), 100) / (dpi.DpiScaleY * 100)).ToReadOnlyReactivePropertySlim();
            Model.Current.SelectedItemObserver.Subscribe(SelectedItemChanged);
        }

        private void SelectedItemChanged(StrategyItem? item)
        {
            if ((item == null) || (item.Image == null) || (item.PlayerX == null) || (item.PlayerY == null))
            {
                SpriteImage.Value = null;
            }
            else
            {
                SpriteImage.Value = item.Image;
                SpriteX.Value = item.PlayerX.Value - 12;
                SpriteY.Value = item.PlayerY.Value + (item.RideOnYoshi ? 17 : 1);
            }
        }

        public void ScrollView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (e.Delta < 0)
                    {
                        switch (Model.Current.ThumbnailScale.Value)
                        {
                            case > 100:
                                Model.Current.ThumbnailScale.Value = 100;
                                break;
                            case > 75:
                                Model.Current.ThumbnailScale.Value = 75;
                                break;
                            default:
                                Model.Current.ThumbnailScale.Value = 50;
                                break;
                        }
                    }
                    else if (e.Delta > 0)
                    {
                        switch (Model.Current.ThumbnailScale.Value)
                        {
                            case < 50:
                                Model.Current.ThumbnailScale.Value = 50;
                                break;
                            case < 75:
                                Model.Current.ThumbnailScale.Value = 75;
                                break;
                            default:
                                Model.Current.ThumbnailScale.Value = 100;
                                break;
                        }
                    }
                }
                else
                {
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - Math.Sign(e.Delta) * 16);
                }
                e.Handled = true;
            }
        }
    }
}
