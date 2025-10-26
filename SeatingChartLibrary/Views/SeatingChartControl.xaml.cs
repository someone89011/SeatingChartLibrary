using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SeatingChartLibrary.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;

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
        private Dictionary<Seat, Point> _selectedOffsets = new Dictionary<Seat, Point>();

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

        private void OnAlignRight(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AlignRight();
        }

        private void OnAlignTop(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AlignTop();
        }

        private void OnAlignBottom(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AlignBottom();
        }

        private void OnSetHorizontalSpacing(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var dialog = new SpacingDialog();
            if (dialog.ShowDialog() == true && dialog.Spacing > 0)
            {
                vm?.SetHorizontalSpacing(dialog.Spacing);
            }
        }

        private void OnSetVerticalSpacing(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var dialog = new SpacingDialog();
            if (dialog.ShowDialog() == true && dialog.Spacing > 0)
            {
                vm?.SetVerticalSpacing(dialog.Spacing);
            }
        }

        private void OnSetSeatNumbers(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            var dialog = new NumberingDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    vm?.SetSeatNumbers(dialog.StartNumber, dialog.IsIncrement);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OnAutoArrangeSeats(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.AutoArrangeSeats(); // �ϥιw�]���j�M�e��
        }

        private void OnSaveToJson(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = ".json"
            };
            if (dialog.ShowDialog() == true)
            {
                vm?.SaveToJson(dialog.FileName);
            }
        }

        private void OnLoadFromJson(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json"
            };
            if (dialog.ShowDialog() == true)
            {
                vm?.LoadFromJson(dialog.FileName);
            }
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
            var rect = new Rect(_rubberBandAdorner.StartPoint, e.GetPosition(ContainerGrid));
            bool isAdd = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            vm?.SelectSeats(rect, isAdd);
            _adornerLayer.Remove(_rubberBandAdorner);
            _rubberBandAdorner = null;
            e.Handled = true;
        }

        private void OnSeatMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.AppMode != AppMode.EditMode) return;
            if (sender is Border border && border.DataContext is Seat seat)
            {
                bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                bool isShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

                if (isCtrl || isShift)
                {
                    seat.IsSelected = !seat.IsSelected;
                    if (seat.IsSelected)
                        vm.SelectedSeats.Add(seat);
                    else
                        vm.SelectedSeats.Remove(seat);
                }
                else
                {
                    if (!seat.IsSelected)
                    {
                        vm?.ClearSelection();
                        seat.IsSelected = true;
                        vm?.SelectedSeats.Add(seat);
                    }
                }

                if (vm.SelectedSeats.Any())
                {
                    _isDragging = true;
                    _draggedSeat = border;
                    var mousePos = e.GetPosition(SeatsCanvas);
                    _dragOffset = new Point(mousePos.X - seat.PositionX, mousePos.Y - seat.PositionY);

                    // Calculate offsets for all selected seats relative to the dragged seat
                    _selectedOffsets.Clear();
                    foreach (var selectedSeat in vm.SelectedSeats)
                    {
                        if (selectedSeat != seat)
                        {
                            double offsetX = selectedSeat.PositionX - seat.PositionX;
                            double offsetY = selectedSeat.PositionY - seat.PositionY;
                            _selectedOffsets[selectedSeat] = new Point(offsetX, offsetY);
                        }
                    }

                    border.CaptureMouse();
                }
                e.Handled = true;
            }
        }

        private void OnSeatMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedSeat == null) return;
            var vm = DataContext as MainViewModel;
            var seat = _draggedSeat.DataContext as Seat;
            var mousePos = e.GetPosition(SeatsCanvas);

            // Move the primary dragged seat
            double newX = Math.Round((mousePos.X - _dragOffset.X) / GridSize) * GridSize;
            double newY = Math.Round((mousePos.Y - _dragOffset.Y) / GridSize) * GridSize;
            double deltaX = newX - seat.PositionX;
            double deltaY = newY - seat.PositionY;

            // Apply delta to all selected seats
            foreach (var selectedSeat in vm.SelectedSeats)
            {
                selectedSeat.PositionX += deltaX;
                selectedSeat.PositionY += deltaY;
            }
        }

        private void OnSeatMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            var vm = DataContext as MainViewModel;
            _draggedSeat.ReleaseMouseCapture();
            _draggedSeat = null;
            _selectedOffsets.Clear();
            e.Handled = true;

            // Check collisions for all selected seats
            foreach (var seat in vm.SelectedSeats.ToList())
            {
                int attempts = 0;
                while (CheckCollision(seat, vm.Seats) && attempts < 10)
                {
                    seat.PositionX += GridSize;
                    attempts++;
                }
                if (attempts >= 10)
                {
                    MessageBox.Show("�L�k��m�y��A��m���|�A�Э��s�վ�C");
                }
            }
        }

        private void OnSeatMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.AppMode != AppMode.EditMode) return;
            if (sender is Border border && border.DataContext is Seat seat)
            {
                bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                bool isShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

                if (isCtrl || isShift)
                {
                    seat.IsSelected = !seat.IsSelected;
                    if (seat.IsSelected)
                        vm.SelectedSeats.Add(seat);
                    else
                        vm.SelectedSeats.Remove(seat);
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

        private void OnRotate15(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.RotateSelectedSeats(15);
        }

        private void OnRotateMinus15(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.RotateSelectedSeats(-15);
        }

        private void OnEditSeat(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Parent is ContextMenu menu && menu.PlacementTarget is Border border && border.DataContext is Seat seat)
            {
                var vm = DataContext as MainViewModel;
                var dialog = new EditSeatDialog(seat, vm.RowNames);
                if (dialog.ShowDialog() == true)
                {
                    vm?.UpdateSeat(seat, dialog.RowName, dialog.Number, dialog.PersonName, dialog.DeviceType, dialog.DeviceNumber);
                }
            }
        }

        private void OnDeleteSeat(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Parent is ContextMenu menu && menu.PlacementTarget is Border border && border.DataContext is Seat seat)
            {
                var vm = DataContext as MainViewModel;
                vm?.Seats.Remove(seat);
                vm?.SelectedSeats.Remove(seat);
            }
        }

        private bool CheckCollision(Seat currentSeat, ObservableCollection<Seat> seats)
        {
            const double seatWidth = 80;
            const double seatHeight = 60;
            var currentRect = new Rect(currentSeat.PositionX, currentSeat.PositionY, seatWidth, seatHeight);
            foreach (var seat in seats)
            {
                if (seat == currentSeat) continue;
                var seatRect = new Rect(seat.PositionX, seat.PositionY, seatWidth, seatHeight);
                if (currentRect.IntersectsWith(seatRect)) return true;
            }
            return false;
        }
    }

    public class RubberBandAdorner : Adorner
    {
        private Point _startPoint;
        private Point _currentPoint;
        private readonly Brush _fill = new SolidColorBrush(Colors.LightBlue) { Opacity = 0.3 };
        private readonly Pen _pen = new Pen(Brushes.Blue, 1);

        public Point StartPoint => _startPoint;

        public RubberBandAdorner(UIElement adornedElement, Point startPoint) : base(adornedElement)
        {
            _startPoint = startPoint;
            _currentPoint = startPoint;
            IsHitTestVisible = false;
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