using Microsoft.Toolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using SmwAceHelper.Models;
using SmwAceHelper.Properties;
using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SmwAceHelper.ViewModels
{
    public class ZoomViewModel : ObservableObject
    {
        public ReactivePropertySlim<BitmapImage?> SpriteImage { get; } = new ReactivePropertySlim<BitmapImage?>();
        public ReactivePropertySlim<double> SpriteX { get; } = new ReactivePropertySlim<double>();
        public ReactivePropertySlim<double> SpriteY { get; } = new ReactivePropertySlim<double>();
        public ReactivePropertySlim<double> BackgroundX { get; } = new ReactivePropertySlim<double>();
        public ReactivePropertySlim<double> BackgroundY { get; } = new ReactivePropertySlim<double>();
        public ReadOnlyReactivePropertySlim<double> ScaleX { get; }
        public ReadOnlyReactivePropertySlim<double> ScaleY { get; }

        public ZoomViewModel()
        {
            ScaleX = Model.Current.ZoomScale.CombineLatest(Model.Current.DpiScale, (scale, dpi) => Math.Min(Math.Max(4, scale), 8) / dpi.DpiScaleX).ToReadOnlyReactivePropertySlim();
            ScaleY = Model.Current.ZoomScale.CombineLatest(Model.Current.DpiScale, (scale, dpi) => Math.Min(Math.Max(4, scale), 8) / dpi.DpiScaleY).ToReadOnlyReactivePropertySlim();
            Model.Current.SelectedItemObserver.Subscribe(SelectedItemChanged);
        }

        private void SelectedItemChanged(StrategyItem? item)
        {
            if ((item == null) || (item.Image == null) || (item.PlayerX == null) || (item.PlayerY == null))
            {
                SpriteImage.Value = null;
                BackgroundX.Value = 0;
                BackgroundY.Value = 0;
            }
            else
            {
                int sx = 12;
                int sy = 17;
                int bx = 24 - item.PlayerX.Value;
                int by = (item.RideOnYoshi ? 0 : 16) - item.PlayerY.Value;

                AdjustOffset(ref sx, ref bx, 64 - Resources.YI2.Width);
                AdjustOffset(ref sy, ref by, 64 - Resources.YI2.Height);

                SpriteImage.Value = item.Image;
                SpriteX.Value = sx;
                SpriteY.Value = sy;
                BackgroundX.Value = bx;
                BackgroundY.Value = by;
            }
        }

        private void AdjustOffset(ref int sprite, ref int background, int min)
        {
            if (background > 0)
            {
                sprite -= background;
                background = 0;
            }
            else if (background < min)
            {
                sprite += min - background;
                background = min;
            }
        }

        public void Canvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                int scale = Model.Current.ZoomScale.Value + Math.Sign(e.Delta);
                scale = Math.Min(Math.Max(4, scale), 8);
                Model.Current.ZoomScale.Value = scale;
                e.Handled = true;
            }
        }
    }
}
