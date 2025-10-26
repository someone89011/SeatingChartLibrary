using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.IO;
using System.Text.Json;

namespace SeatingChartLibrary.ViewModels
{
    public enum AppMode { ViewMode, EditMode }

    public class Seat : INotifyPropertyChanged
    {
        private string _rowName;
        private string _number;
        private double _positionX;
        private double _positionY;
        private double _angle;
        private Person _person;
        private bool _isSelected;

        public string RowName { get => _rowName; set { _rowName = value; OnPropertyChanged(nameof(RowName)); } }
        public string Number { get => _number; set { _number = value; OnPropertyChanged(nameof(Number)); } }
        public double PositionX { get => _positionX; set { _positionX = value; OnPropertyChanged(nameof(PositionX)); } }
        public double PositionY { get => _positionY; set { _positionY = value; OnPropertyChanged(nameof(PositionY)); } }
        public double Angle { get => _angle; set { _angle = value; OnPropertyChanged(nameof(Angle)); } }
        public Person Person { get => _person; set { _person = value; OnPropertyChanged(nameof(Person)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }

        public void Rotate(double delta) => Angle = (Angle + delta) % 360;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Person : INotifyPropertyChanged
    {
        private string _name;
        private Device _device;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public Device Device { get => _device; set { _device = value; OnPropertyChanged(nameof(Device)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Device : INotifyPropertyChanged
    {
        private string _type;
        private string _number;

        public string Type { get => _type; set { _type = value; OnPropertyChanged(nameof(Type)); } }
        public string Number { get => _number; set { _number = value; OnPropertyChanged(nameof(Number)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private AppMode _appMode = AppMode.EditMode;

        public ObservableCollection<string> RowNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<Seat> Seats { get; } = new ObservableCollection<Seat>();
        public ObservableCollection<Seat> SelectedSeats { get; } = new ObservableCollection<Seat>();

        public AppMode AppMode { get => _appMode; set { _appMode = value; OnPropertyChanged(nameof(AppMode)); } }

        public void SetMode(AppMode mode) => AppMode = mode;

        public void SaveToJson(string path)
        {
            var data = new SaveData { RowNames = RowNames, Seats = Seats };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public void LoadFromJson(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveData>(json);
            RowNames.Clear();
            foreach (var row in data.RowNames ?? new ObservableCollection<string>())
                RowNames.Add(row);
            Seats.Clear();
            foreach (var seat in data.Seats ?? new ObservableCollection<Seat>())
                Seats.Add(seat);
        }

        public void AddRowName(string name)
        {
            if (AppMode != AppMode.EditMode) return;
            if (!RowNames.Contains(name))
                RowNames.Add(name);
        }

        public void DeleteRowName(string name)
        {
            if (AppMode != AppMode.EditMode) return;
            if (!Seats.Any(s => s.RowName == name))
                RowNames.Remove(name);
        }

        public void AddEmptySeat(double x, double y)
        {
            if (AppMode != AppMode.EditMode) return;
            if (Seats.Count >= 1000) throw new InvalidOperationException("超過1000個座位限制");
            var newSeat = new Seat { PositionX = x, PositionY = y }; // 不設置 Number
            Seats.Add(newSeat);
        }

        public void UpdateSeat(Seat seat, string rowName, string number, string personName, string deviceType, string deviceNumber)
        {
            if (AppMode != AppMode.EditMode) return;
            if (!string.IsNullOrEmpty(rowName) && !string.IsNullOrEmpty(number) &&
                Seats.Any(s => s != seat && s.RowName == rowName && s.Number == number))
                throw new InvalidOperationException("座位 (RowName, Number) 重複");
            seat.RowName = string.IsNullOrEmpty(rowName) ? null : rowName;
            seat.Number = string.IsNullOrEmpty(number) ? null : number;
            seat.Person = string.IsNullOrEmpty(personName) ? null : new Person
            {
                Name = personName,
                Device = string.IsNullOrEmpty(deviceType) ? null : new Device { Type = deviceType, Number = deviceNumber }
            };
        }

        public void AlignLeft()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var minX = SelectedSeats.Min(s => s.PositionX);
            foreach (var seat in SelectedSeats)
                seat.PositionX = minX;
        }

        public void AlignRight()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var maxX = SelectedSeats.Max(s => s.PositionX);
            foreach (var seat in SelectedSeats)
                seat.PositionX = maxX;
        }

        public void AlignTop()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var minY = SelectedSeats.Min(s => s.PositionY);
            foreach (var seat in SelectedSeats)
                seat.PositionY = minY;
        }

        public void AlignBottom()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var maxY = SelectedSeats.Max(s => s.PositionY);
            foreach (var seat in SelectedSeats)
                seat.PositionY = maxY;
        }

        public void SetHorizontalSpacing(double spacing)
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var sortedSeats = SelectedSeats.OrderBy(s => s.PositionY).ThenBy(s => s.PositionX).ToList();
            double startX = sortedSeats.First().PositionX;
            double currentY = sortedSeats.First().PositionY;
            int index = 0;
            foreach (var seat in sortedSeats)
            {
                if (Math.Abs(seat.PositionY - currentY) > 10) // 新行
                {
                    startX = sortedSeats.First().PositionX;
                    currentY = seat.PositionY;
                    index = 0;
                }
                seat.PositionX = startX + index * spacing;
                index++;
            }
        }

        public void SetVerticalSpacing(double spacing)
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var sortedSeats = SelectedSeats.OrderBy(s => s.PositionX).ThenBy(s => s.PositionY).ToList();
            double startY = sortedSeats.First().PositionY;
            double currentX = sortedSeats.First().PositionX;
            int index = 0;
            foreach (var seat in sortedSeats)
            {
                if (Math.Abs(seat.PositionX - currentX) > 10) // 新列
                {
                    startY = sortedSeats.First().PositionY;
                    currentX = seat.PositionX;
                    index = 0;
                }
                seat.PositionY = startY + index * spacing;
                index++;
            }
        }

        public void RotateSelectedSeats(double delta)
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            foreach (var seat in SelectedSeats)
            {
                seat.Rotate(delta);
            }
        }

        public void SelectSeats(Rect selectionRect, bool isAdd = false)
        {
            if (AppMode != AppMode.EditMode) return;
            const double seatWidth = 80;
            const double seatHeight = 60;
            if (!isAdd)
            {
                ClearSelection();
            }
            foreach (var seat in Seats)
            {
                var seatRect = new Rect(seat.PositionX, seat.PositionY, seatWidth, seatHeight);
                if (selectionRect.IntersectsWith(seatRect))
                {
                    if (!seat.IsSelected)
                    {
                        seat.IsSelected = true;
                        SelectedSeats.Add(seat);
                    }
                }
                else if (!isAdd)
                {
                    seat.IsSelected = false;
                }
            }
        }

        public void ClearSelection()
        {
            if (AppMode != AppMode.EditMode) return;
            foreach (var seat in SelectedSeats)
                seat.IsSelected = false;
            SelectedSeats.Clear();
        }

        public void SetSeatNumbers(int startNumber, bool isIncrement)
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            var sortedSeats = SelectedSeats.OrderBy(s => s.PositionY).ThenBy(s => s.PositionX).ToList();
            int currentNumber = startNumber;
            foreach (var seat in sortedSeats)
            {
                string newNumber = currentNumber.ToString();
                if (Seats.Any(s => s != seat && s.RowName == seat.RowName && s.Number == newNumber))
                {
                    throw new InvalidOperationException($"座位 (RowName: {seat.RowName}, Number: {newNumber}) 已存在");
                }
                seat.Number = newNumber;
                currentNumber += isIncrement ? 1 : -1;
            }
        }

        public void AutoArrangeSeats(double spacingX = 90, double spacingY = 70, double maxWidth = 800)
        {
            if (AppMode != AppMode.EditMode) return;
            double x = 0, y = 0;
            foreach (var seat in Seats)
            {
                seat.PositionX = x;
                seat.PositionY = y;
                x += spacingX;
                if (x >= maxWidth - spacingX)
                {
                    x = 0;
                    y += spacingY;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SaveData
    {
        public ObservableCollection<string> RowNames { get; set; }
        public ObservableCollection<Seat> Seats { get; set; }
    }
}