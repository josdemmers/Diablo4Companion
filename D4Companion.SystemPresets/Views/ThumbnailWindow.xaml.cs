using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D4Companion.SystemPresets.Messages;
using D4Companion.SystemPresets.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace D4Companion.SystemPresets.Views
{
    /// <summary>
    /// Interaction logic for ThumbnailWindow.xaml
    /// </summary>
    public partial class ThumbnailWindow : Window
    {
        private const int WM_SIZING = 0x0214;
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private DrawingGroup _drawingGroup = new DrawingGroup();

        #region Constructors

        public ThumbnailWindow(HWND handleSource)
        {
            DataContext = App.Current.Services.GetRequiredService<ThumbnailWindowViewModel>();
            InitializeComponent();

            ((ThumbnailWindowViewModel)DataContext).HandleSource = handleSource;

            // Init messages
            WeakReferenceMessenger.Default.Register<CursorUpdatedMessage>(this, HandleCursorUpdatedMessage);
        }

        #endregion

        #region Events

        #endregion

        #region Properties

        #endregion

        #region Event handlers

        private void HandleCursorUpdatedMessage(object recipient, CursorUpdatedMessage message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdateRender();
            });            
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(_drawingGroup);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((ThumbnailWindowViewModel)DataContext).ClosingHandler();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
            var hWnd = windowInteropHelper.Handle;
            ((ThumbnailWindowViewModel)DataContext).Handle = (HWND)hWnd;
            ((ThumbnailWindowViewModel)DataContext).Init();

            //var extendedStyle = PInvoke.GetWindowLong((HWND)hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            //int result = PInvoke.SetWindowLong((HWND)hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, extendedStyle | (int)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);

            HwndSource.FromHwnd(hWnd)?.AddHook(WndProc);

            UpdateActualSize();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateActualSize();
        }

        #endregion

        #region Methods

        // The aspect ratio is enforced here, in WM_SIZING, rather than by re-setting
        // Width/Height from Window_SizeChanged.
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {
                double ratio = ((ThumbnailWindowViewModel)DataContext).Ratio;
                if (ratio > 0)
                {
                    RECT rect = Marshal.PtrToStructure<RECT>(lParam);
                    int width = rect.right - rect.left;
                    int height = rect.bottom - rect.top;

                    switch (wParam.ToInt32())
                    {
                        case WMSZ_LEFT:
                        case WMSZ_RIGHT:
                            rect.bottom = rect.top + (int)Math.Round(width / ratio);
                            break;
                        case WMSZ_TOP:
                        case WMSZ_BOTTOM:
                            rect.right = rect.left + (int)Math.Round(height * ratio);
                            break;
                        case WMSZ_TOPLEFT:
                            rect.left = rect.right - (int)Math.Round(height * ratio);
                            break;
                        case WMSZ_TOPRIGHT:
                            rect.top = rect.bottom - (int)Math.Round(width / ratio);
                            break;
                        case WMSZ_BOTTOMLEFT:
                            rect.bottom = rect.top + (int)Math.Round(width / ratio);
                            break;
                        case WMSZ_BOTTOMRIGHT:
                        default:
                            rect.bottom = rect.top + (int)Math.Round(width / ratio);
                            break;
                    }

                    Marshal.StructureToPtr(rect, lParam, true);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void UpdateActualSize()
        {
            if (double.IsNaN(Height) || double.IsNaN(Width)) return;

            IEnumerable children = LogicalTreeHelper.GetChildren(this);
            double height = 0;
            double width = 0;

            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject? depChild = child as DependencyObject;
                    if (depChild != null)
                    {
                        height = ((FrameworkElement)depChild).ActualHeight;
                        width = ((FrameworkElement)depChild).ActualWidth;
                    }
                }
            }

            ((ThumbnailWindowViewModel)DataContext).ActualHeight = height;
            ((ThumbnailWindowViewModel)DataContext).ActualWidth = width;
            ((ThumbnailWindowViewModel)DataContext).ActualHeightPixels = this.PointToScreen(new Point(width, height)).Y - this.PointToScreen(new Point(0, 0)).Y;
            ((ThumbnailWindowViewModel)DataContext).ActualWidthPixels = this.PointToScreen(new Point(width, height)).X - this.PointToScreen(new Point(0, 0)).X;

            // Note: Alternative using visual's own DPI
            //DpiScale dpi = VisualTreeHelper.GetDpi(this);
            //((ThumbnailWindowViewModel)DataContext).ActualHeightPixels = height * dpi.DpiScaleY;
            //((ThumbnailWindowViewModel)DataContext).ActualWidthPixels = width * dpi.DpiScaleX;

            ((ThumbnailWindowViewModel)DataContext).RefreshThumbnailDestination();
        }

        private void UpdateRender()
        {
            _drawingGroup.Children.Clear();

            var mouseXPercent = ((ThumbnailWindowViewModel)DataContext).MouseXPercent;
            var mouseYPercent = ((ThumbnailWindowViewModel)DataContext).MouseYPercent;

            // Convert to DIPs
            var viewModel = (ThumbnailWindowViewModel)DataContext;
            double wpfX = mouseXPercent * viewModel.ActualWidth / 100.0;
            double wpfY = mouseYPercent * viewModel.ActualHeight / 100.0;

            _drawingGroup.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Red, 2), new LineGeometry(new Point(0, wpfY), new Point(ActualWidth, wpfY))));
            _drawingGroup.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Red, 2), new LineGeometry(new Point(wpfX, 0), new Point(wpfX, ActualHeight))));
        }

        #endregion        
    }
}
