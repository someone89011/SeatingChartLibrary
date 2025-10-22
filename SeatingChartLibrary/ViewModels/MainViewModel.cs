using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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

        public string RowName { get => _rowName; set { _rowName = value; OnPropertyChanged(nameof(RowName)); } }
        public string Number { get => _number; set { _number = value; OnPropertyChanged(nameof(Number)); } }
        public double PositionX { get => _positionX; set { _positionX = value; OnPropertyChanged(nameof(PositionX)); } }
        public double PositionY { get => _positionY; set { _positionY = value; OnPropertyChanged(nameof(PositionY)); } }
        public double Angle { get => _angle; set { _angle = value; OnPropertyChanged(nameof(Angle)); } }
        public Person Person { get => _person; set { _person = value; OnPropertyChanged(nameof(Person)); } }

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
        public AppMode AppMode { get => _appMode; set { _appMode = value; OnPropertyChanged(nameof(AppMode)); } }

        public void SetMode(AppMode mode) => AppMode = mode;

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
            Seats.Add(new Seat { PositionX = x, PositionY = y });
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

        public void AlignLeft(Seat[] selectedSeats)
        {
            if (AppMode != AppMode.EditMode || !selectedSeats.Any()) return;
            var minX = selectedSeats.Min(s => s.PositionX);
            foreach (var seat in selectedSeats)
                seat.PositionX = minX;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}