using D4Companion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace D4Companion.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private int _dragStartPixelX;
        private int _dragStartPixelY;
        private int _dragStartRoiX;
        private int _dragStartRoiY;
        private bool _isDragging;

        // Start of Constructors region

        #region Constructors

        public DashboardView()
        {
            DataContext = App.Current.Services.GetRequiredService<DashboardViewModel>();

            InitializeComponent();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void ScreenshotImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var viewmodel = ((DashboardViewModel)DataContext);
            if (viewmodel.SelectedThumbnailEdit == null) return;

            if (!TryGetPixelPosition(e, out int pixelX, out int pixelY)) return;

            if (e.ClickCount != 2)
            {
                // Start dragging the ROI
                _isDragging = true;
                _dragStartPixelX = pixelX;
                _dragStartPixelY = pixelY;
                _dragStartRoiX = viewmodel.IsSourceROIActive ? viewmodel.SelectedThumbnailEdit.SourcePositionX : viewmodel.SelectedThumbnailEdit.DestinationPositionX;
                _dragStartRoiY = viewmodel.IsSourceROIActive ? viewmodel.SelectedThumbnailEdit.SourcePositionY : viewmodel.SelectedThumbnailEdit.DestinationPositionY;
                ScreenshotImage.CaptureMouse();
                return;
            }

            _isDragging = false;
            if (viewmodel.IsSourceROIActive)
            {
                viewmodel.SelectedThumbnailEdit.SourcePositionX = pixelX;
                viewmodel.SelectedThumbnailEdit.SourcePositionY = pixelY;
            }
            else
            {
                viewmodel.SelectedThumbnailEdit.DestinationPositionX = pixelX;
                viewmodel.SelectedThumbnailEdit.DestinationPositionY = pixelY;
            }
        }

        private void ScreenshotImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopDragging();
        }

        private void ScreenshotImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopDragging();
                return;
            }

            var viewmodel = ((DashboardViewModel)DataContext);
            if (viewmodel.SelectedThumbnailEdit == null) return;

            if (!TryGetPixelPosition(e, out int pixelX, out int pixelY)) return;

            if (ScreenshotImage.Source is not BitmapSource bitmapSource) return;

            var thumbnail = viewmodel.SelectedThumbnailEdit;
            int roiWidth = viewmodel.IsSourceROIActive ? thumbnail.SourceWidth : thumbnail.DestinationWidth;
            int roiHeight = viewmodel.IsSourceROIActive ? thumbnail.SourceHeight : thumbnail.DestinationHeight;

            // Keep the whole ROI inside the image
            int maxX = Math.Max(0, bitmapSource.PixelWidth - roiWidth);
            int maxY = Math.Max(0, bitmapSource.PixelHeight - roiHeight);
            int newX = Math.Clamp(_dragStartRoiX + (pixelX - _dragStartPixelX), 0, maxX);
            int newY = Math.Clamp(_dragStartRoiY + (pixelY - _dragStartPixelY), 0, maxY);

            if (viewmodel.IsSourceROIActive)
            {
                viewmodel.SelectedThumbnailEdit.SourcePositionX = newX;
                viewmodel.SelectedThumbnailEdit.SourcePositionY = newY;
            }
            else
            {
                viewmodel.SelectedThumbnailEdit.DestinationPositionX = newX;
                viewmodel.SelectedThumbnailEdit.DestinationPositionY = newY;
            }
        }

        private void TextBoxThumbnail_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxThumbnailWatermark.Visibility = Visibility.Collapsed;
        }

        private void TextBoxThumbnail_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxThumbnail.Text))
            {
                TextBoxThumbnailWatermark.Visibility = Visibility.Visible;
            }
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void StopDragging()
        {
            _isDragging = false;
            ScreenshotImage.ReleaseMouseCapture();
        }

        private bool TryGetPixelPosition(MouseEventArgs e, out int pixelX, out int pixelY)
        {
            pixelX = 0;
            pixelY = 0;

            if (ScreenshotImage.Source is not BitmapSource bitmapSource) return false;
            if (ScreenshotImage.ActualWidth <= 0 || ScreenshotImage.ActualHeight <= 0) return false;

            // Position in WPF DIPs
            Point position = e.GetPosition(ScreenshotImage);

            // Scale DIP to pixel
            double scaleX = bitmapSource.PixelWidth / ScreenshotImage.ActualWidth;
            double scaleY = bitmapSource.PixelHeight / ScreenshotImage.ActualHeight;

            pixelX = Math.Clamp((int)(position.X * scaleX), 0, bitmapSource.PixelWidth - 1);
            pixelY = Math.Clamp((int)(position.Y * scaleY), 0, bitmapSource.PixelHeight - 1);
            return true;
        }

        #endregion
    }
}
