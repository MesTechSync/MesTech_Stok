using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class ProductImageViewer : Window
    {
        private readonly List<BitmapImage> _images;
        private int _index = 0;

        public ProductImageViewer(IEnumerable<BitmapImage> images)
        {
            InitializeComponent();
            _images = images?.ToList() ?? new List<BitmapImage>();
            Thumbs.ItemsSource = _images;
            if (_images.Count > 0) MainImage.Source = _images[0];
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            else if (e.Key == Key.Right) Next();
            else if (e.Key == Key.Left) Prev();
            base.OnKeyDown(e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void Next()
        {
            if (_images.Count == 0) return;
            _index = (_index + 1) % _images.Count;
            MainImage.Source = _images[_index];
        }

        private void Prev()
        {
            if (_images.Count == 0) return;
            _index = (_index - 1 + _images.Count) % _images.Count;
            MainImage.Source = _images[_index];
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Thumb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as System.Windows.Controls.Image)?.Source is BitmapImage bmp)
            {
                MainImage.Source = bmp;
                _index = _images.IndexOf(bmp);
            }
        }

        private void MainImage_MouseClick(object sender, MouseButtonEventArgs e)
        {
            // Görsel üzerinde çift tık ile kapat
            if (e.ClickCount >= 2)
            {
                Close();
                return;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e) => Next();
        private void Prev_Click(object sender, RoutedEventArgs e) => Prev();

        private double _zoom = 1.0;
        private bool _panning;
        private Point _panStart;

        private void MainImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double step = 0.1;
            _zoom = Math.Max(0.2, Math.Min(6.0, _zoom + (e.Delta > 0 ? step : -step)));
            MainImage.LayoutTransform = new System.Windows.Media.ScaleTransform(_zoom, _zoom);
        }

        private void MainImage_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = Scroller;
            e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;
        }

        private void MainImage_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Pinch zoom
            var scaleDelta = e.DeltaManipulation.Scale;
            var delta = (scaleDelta.X + scaleDelta.Y) / 2.0;
            if (!double.IsNaN(delta) && !double.IsInfinity(delta) && Math.Abs(delta - 1.0) > 0.001)
            {
                var newZoom = _zoom * delta;
                newZoom = Math.Max(0.2, Math.Min(6.0, newZoom));
                if (Math.Abs(newZoom - _zoom) > 0.0001)
                {
                    _zoom = newZoom;
                    MainImage.LayoutTransform = new ScaleTransform(_zoom, _zoom);
                }
            }

            // Translate (pan)
            var trans = e.DeltaManipulation.Translation;
            if (trans.X != 0 || trans.Y != 0)
            {
                Scroller.ScrollToHorizontalOffset(Scroller.HorizontalOffset - trans.X);
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - trans.Y);
            }

            e.Handled = true;
        }

        private void MainImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _panning = true;
            _panStart = e.GetPosition(Scroller);
            MainImage.CaptureMouse();
            // Tek tıkta kapama için değil; çift tık kapatmayı MouseClick event'inde ele alıyoruz.
        }

        private void MainImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _panning = false;
            MainImage.ReleaseMouseCapture();
        }

        private void MainImage_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_panning) return;
            var current = e.GetPosition(Scroller);
            var delta = current - _panStart;
            _panStart = current;
            Scroller.ScrollToHorizontalOffset(Scroller.HorizontalOffset - delta.X);
            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - delta.Y);
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Arka plan tıklandığında (beyaz kart değil) kapatmak için hit test yap
            try
            {
                var src = e.OriginalSource as DependencyObject;
                bool insideCard = false;
                while (src != null)
                {
                    if (src is Border b && b.CornerRadius != new CornerRadius(0) && b.Background is SolidColorBrush)
                    {
                        insideCard = true; break;
                    }
                    src = VisualTreeHelper.GetParent(src);
                }
                if (!insideCard) Close();
            }
            catch { }
        }
    }
}

