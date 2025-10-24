using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SeatingChartLibrary.ViewModels;

namespace SeatingChartLibrary.Views
{
    public partial class SeatingChartControl : UserControl
    {
        private bool _isDragging;
        private bool _isSelecting;
        private Border _draggedSeat;
        private Point _dragOffset;
        private Point _selectionStart;
        private RubberBandAdorner _rubberBandAdorner;
        private AdornerLayer _adornerLayer;
        private const double GridSize = 10;

        public SeatingChartControl()
        {
            InitializeComponent();
            _adornerLayer = AdornerLayer.GetAdornerLayer(ContainerGrid);
        }

        private void OnAddRowName(object sender, RoutedEventArgs e)
        {
            var dialog = new AddRowDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.RowName))
            {
                var vm = DataContext as MainViewModel;
                vm?.AddRowName(dialog.RowName);
            }
        }

        private void OnAddEmptySeat(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AddEmptySeat(100, 100);
        }

        private void OnAlignLeft(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AlignLeft();
        }

        private void OnGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.AppMode != AppMode.EditMode) return;

            var mousePos = e.GetPosition(ContainerGrid);
            var hitTestResult = VisualTreeHelper.HitTest(ContainerGrid, mousePos);
            if (hitTestResult.VisualHit is Canvas)
            {
                _isSelecting = true;
                _selectionStart = mousePos;
                _rubberBandAdorner = new RubberBandAdorner(ContainerGrid, _selectionStart);
                _adornerLayer.Add(_rubberBandAdorner);
                ContainerGrid.CaptureMouse();
                vm?.ClearSelection();
                e.Handled = true;
            }
        }

        private void OnGridMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var currentPos = e.GetPosition(ContainerGrid);
            _rubberBandAdorner.UpdatePosition(currentPos);
        }

        private void OnGridMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;

            _isSelecting = false;
            ContainerGrid.ReleaseMouseCapture();

            var vm = DataContext as MainViewModel;
            var rect = new Rect(
                _rubberBandAdorner.StartPoint,
                e.GetPosition(ContainerGrid));
            vm?.SelectSeats(rect);

            _adornerLayer.Remove(_rubberBandAdorner);
            _rubberBandAdorner = null;
            e.Handled = true;
        }

        private void OnSeatMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.AppMode != AppMode.EditMode) return;

            if (sender is Border border)
            {
                _isDragging = true;
                _draggedSeat = border;
                var seat = border.DataContext as Seat;
                var mousePos = e.GetPosition(SeatsCanvas);
                _dragOffset = new Point(mousePos.X - seat.PositionX, mousePos.Y - seat.PositionY);
                border.CaptureMouse();

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (!seat.IsSelected)
                    {
                        seat.IsSelected = true;
                        vm.SelectedSeats.Add(seat);
                    }
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (!seat.IsSelected)
                    {
                        seat.IsSelected = true;
                        vm.SelectedSeats.Add(seat);
                    }
                }
                else
                {
                    vm?.ClearSelection();
                    seat.IsSelected = true;
                    vm?.SelectedSeats.Add(seat);
                }
                e.Handled = true;
            }
        }

        private void OnSeatMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedSeat == null) return;

            var seat = _draggedSeat.DataContext as Seat;
            var mousePos = e.GetPosition(SeatsCanvas);

            seat.PositionX = Math.Round((mousePos.X - _dragOffset.X) / GridSize) * GridSize;
            seat.PositionY = Math.Round((mousePos.Y - _dragOffset.Y) / GridSize) * GridSize;
        }

        private void OnSeatMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedSeat?.ReleaseMouseCapture();
                _draggedSeat = null;
                e.Handled = true;

                var vm = DataContext as MainViewModel;
                var seat = _draggedSeat.DataContext as Seat;
                if (CheckCollision(seat, vm.Seats))
                {
                    // 調整位置, e.g., seat.PositionX += 10;
                }
            }
        }

        private bool CheckCollision(Seat currentSeat, ObservableCollection<Seat> seats)
        {
            var currentRect = new Rect(currentSeat.PositionX, currentSeat.PositionY, 80, 60);
            foreach (var seat in seats)
            {
                if (seat == currentSeat) continue;
                var seatRect = new Rect(seat.PositionX, seat.PositionY, 80, 60);
                if (currentRect.IntersectsWith(seatRect)) return true;
            }
            return false;
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
                vm?.UpdateSeat(seat, RowCombo.SelectedItem as string ?? "第一行", "1", "張三", "筆電", "DEV001");
            }
        }
    }

    public class RubberBandAdorner : Adorner
    {
        private Point _startPoint;
        private Point _currentPoint;
        private readonly Brush _fill = new SolidColorBrush(Colors.LightBlue) { Opacity = 0.3 };
        private readonly Pen _pen = new Pen(new SolidColorBrush(Colors.Blue), 1);

        public Point StartPoint => _startPoint;

        public RubberBandAdorner(UIElement adornedElement, Point startPoint) : base(adornedElement)
        {
            _startPoint = startPoint;
            _currentPoint = startPoint;
            IsHitTestVisible = false; // 不擋事件
        }

        public void UpdatePosition(Point currentPos)
        {
            _currentPoint = currentPos;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var rect = new Rect(_startPoint, _currentPoint);
            drawingContext.DrawRectangle(_fill, _pen, rect);
        }
    }
}