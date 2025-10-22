using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using SeatingChartLibrary.ViewModels;

namespace SeatingChartLibrary.Views
{
    public partial class SeatingChartControl : UserControl
    {
        private bool _isDragging;
        private Point _startPoint;
        private Border _draggedSeat;
        private const double GridSize = 10; // Snap-to-Grid 步進

        public SeatingChartControl()
        {
            InitializeComponent();
        }

        private void OnAddRowName(object sender, RoutedEventArgs e)
        {
            // 對話框輸入 (簡化，實際用 Window)
            var vm = DataContext as MainViewModel;
            vm?.AddRowName($"Row{vm.RowNames.Count + 1}");
        }

        private void OnAddEmptySeat(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AddEmptySeat(100, 100);
        }

        private void OnAlignLeft(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            // 簡化為所有座位；實際需實現多選
            vm?.AlignLeft(vm.Seats.ToArray());
        }

        private void OnSeatMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.AppMode != AppMode.EditMode) return;

            if (sender is Border border)
            {
                _isDragging = true;
                _draggedSeat = border;
                _startPoint = e.GetPosition(SeatsCanvas);
                border.CaptureMouse();
            }
        }

        private void OnSeatMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedSeat == null) return;

            var seat = _draggedSeat.DataContext as Seat;
            var currentPoint = e.GetPosition(SeatsCanvas);
            var deltaX = currentPoint.X - _startPoint.X;
            var deltaY = currentPoint.Y - _startPoint.Y;

            seat.PositionX = Math.Round((seat.PositionX + deltaX) / GridSize) * GridSize;
            seat.PositionY = Math.Round((seat.PositionY + deltaY) / GridSize) * GridSize;

            _startPoint = new Point(seat.PositionX, seat.PositionY); // 更新起點為新位置
        }

        private void OnSeatMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedSeat?.ReleaseMouseCapture();
                _draggedSeat = null;
            }
        }

        private void OnRotate15(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Parent is ContextMenu menu && menu.PlacementTarget is Border border)
            {
                var seat = border.DataContext as Seat;
                seat.Rotate(15);
            }
        }

        private void OnRotateMinus15(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Parent is ContextMenu menu && menu.PlacementTarget is Border border)
            {
                var seat = border.DataContext as Seat;
                seat.Rotate(-15);
            }
        }

        private void OnEditSeat(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Parent is ContextMenu menu && menu.PlacementTarget is Border border)
            {
                var vm = DataContext as MainViewModel;
                var seat = border.DataContext as Seat;
                // 對話框編輯 (簡化)
                vm?.UpdateSeat(seat, RowCombo.SelectedItem as string ?? "第一行", "1", "張三", "筆電", "DEV001");
            }
        }
    }

    // Converter for Mode to Visibility (add to resources in XAML or code)
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is AppMode mode && parameter is string param)
            {
                return mode.ToString() == param ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}