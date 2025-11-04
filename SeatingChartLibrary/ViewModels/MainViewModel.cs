using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace SeatingChartLibrary.ViewModels
{
    public enum AppMode { ViewMode, EditMode }

    public class Row
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
    }

    public class Seat : INotifyPropertyChanged
    {
        private string _rowUuid = "";
        private string _seatUuid = Guid.NewGuid().ToString();
        private string _rowName = "";
        private string _number = "";
        private double _positionX;
        private double _positionY;
        private double _angle;
        private Person? _person;
        private bool _isSelected;

        public string SeatUuid { get => _seatUuid; set { _seatUuid = value; OnPropertyChanged(); } }
        public string RowUuid { get => _rowUuid; set { _rowUuid = value; OnPropertyChanged(); } }
        public string RowName { get => _rowName; set { _rowName = value; OnPropertyChanged(); } }
        public string Number { get => _number; set { _number = value; OnPropertyChanged(); } }
        public double PositionX { get => _positionX; set { _positionX = value; OnPropertyChanged(); } }
        public double PositionY { get => _positionY; set { _positionY = value; OnPropertyChanged(); } }
        public double Angle { get => _angle; set { _angle = value; OnPropertyChanged(); } }
        public Person? Person { get => _person; set { _person = value; OnPropertyChanged(); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Person : INotifyPropertyChanged
    {
        private int _participantId;
        private string _name = "";
        private Device? _device;
        private bool _isAttending;
        private bool _isOnline;
        private string _status = "Absent";

        public int ParticipantId { get => _participantId; set { _participantId = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public Device? Device { get => _device; set { _device = value; OnPropertyChanged(); } }
        public bool IsAttending { get => _isAttending; set { _isAttending = value; OnPropertyChanged(); } }
        public bool IsOnline { get => _isOnline; set { _isOnline = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Device : INotifyPropertyChanged
    {
        private string _deviceUuid = Guid.NewGuid().ToString();
        private string _typeUuid = "";
        private string _specificDataJson = "{}";

        public string DeviceUuid { get => _deviceUuid; set { _deviceUuid = value; OnPropertyChanged(); } }
        public string TypeUuid { get => _typeUuid; set { _typeUuid = value; OnPropertyChanged(); } }
        public string SpecificDataJson { get => _specificDataJson; set { _specificDataJson = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly string _dbPath = "meeting.db";
        private readonly int _projectId = 1;
        private SqliteConnection? _conn;

        private AppMode _appMode = AppMode.EditMode;
        private bool _showRowName = true;
        private bool _showDevice = true;

        public ObservableCollection<Row> Rows { get; } = new();
        public ObservableCollection<Seat> Seats { get; } = new();
        public ObservableCollection<Seat> SelectedSeats { get; } = new();

        public AppMode AppMode { get => _appMode; set { _appMode = value; OnPropertyChanged(); } }
        public bool ShowRowName { get => _showRowName; set { _showRowName = value; OnPropertyChanged(); } }
        public bool ShowDevice { get => _showDevice; set { _showDevice = value; OnPropertyChanged(); } }

        public MainViewModel()
        {
            Batteries.Init(); // Initialize SQLite provider
            InitializeDatabase();
            LoadData();
        }

        public MainViewModel(string dbPath, int projectId)
        {
            Batteries.Init(); // Initialize SQLite provider
            _dbPath = dbPath;
            _projectId = projectId;
            InitializeDatabase();
            LoadData();
        }

        public void SetMode(AppMode mode)
        {
            AppMode = mode;
        }

        private void InitializeDatabase()
        {
            _conn = new SqliteConnection($"Data Source={_dbPath}");
            _conn.Open();
            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;
                CREATE TABLE IF NOT EXISTS rows (
                    row_uuid TEXT PRIMARY KEY,
                    project_id INTEGER NOT NULL,
                    row_name TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS seats (
                    seat_uuid TEXT PRIMARY KEY,
                    project_id INTEGER NOT NULL,
                    row_uuid TEXT,
                    seat_no INTEGER NOT NULL,
                    participant_id INTEGER,
                    device_record_uuid TEXT,
                    position_x REAL DEFAULT 0,
                    position_y REAL DEFAULT 0,
                    angle REAL DEFAULT 0,
                    UNIQUE(row_uuid, seat_no)
                );
                CREATE TABLE IF NOT EXISTS devices (
                    device_uuid TEXT PRIMARY KEY,
                    project_id INTEGER NOT NULL,
                    type_uuid TEXT NOT NULL,
                    specific_data TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS participants (
                    participant_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    project_id INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    is_attending INTEGER DEFAULT 0,
                    is_online INTEGER DEFAULT 0,
                    status TEXT DEFAULT 'Absent'
                );
            ";
            cmd.ExecuteNonQuery();
        }

        private void LoadData()
        {
            Rows.Clear();
            Seats.Clear();
            SelectedSeats.Clear();

            var cmd = _conn!.CreateCommand();
            cmd.CommandText = "SELECT row_uuid, row_name FROM rows WHERE project_id = @pid";
            cmd.Parameters.AddWithValue("@pid", _projectId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Rows.Add(new Row { Uuid = reader.GetString(0), Name = reader.GetString(1) });
            }
            reader.Close();

            cmd.Parameters.Clear();

            cmd.CommandText = @"
                SELECT s.seat_uuid, s.row_uuid, r.row_name, s.seat_no, s.position_x, s.position_y, s.angle, p.participant_id, p.name, p.is_attending, p.is_online, p.status, d.device_uuid, d.type_uuid, d.specific_data
                FROM seats s
                LEFT JOIN rows r ON s.row_uuid = r.row_uuid
                LEFT JOIN participants p ON s.participant_id = p.participant_id
                LEFT JOIN devices d ON s.device_record_uuid = d.device_uuid
                WHERE s.project_id = @pid
            ";
            cmd.Parameters.AddWithValue("@pid", _projectId);
            using var seatReader = cmd.ExecuteReader();
            while (seatReader.Read())
            {
                var seat = new Seat
                {
                    SeatUuid = seatReader.GetString(0),
                    RowUuid = seatReader.IsDBNull(1) ? "" : seatReader.GetString(1),
                    RowName = seatReader.IsDBNull(2) ? "" : seatReader.GetString(2),
                    Number = seatReader.GetInt32(3).ToString(),
                    PositionX = seatReader.GetDouble(4),
                    PositionY = seatReader.GetDouble(5),
                    Angle = seatReader.GetDouble(6),
                    Person = seatReader.IsDBNull(7) ? null : new Person
                    {
                        ParticipantId = seatReader.GetInt32(7),
                        Name = seatReader.GetString(8),
                        IsAttending = seatReader.GetBoolean(9),
                        IsOnline = seatReader.GetBoolean(10),
                        Status = seatReader.GetString(11),
                        Device = seatReader.IsDBNull(12) ? null : new Device
                        {
                            DeviceUuid = seatReader.GetString(12),
                            TypeUuid = seatReader.GetString(13),
                            SpecificDataJson = seatReader.GetString(14)
                        }
                    }
                };
                Seats.Add(seat);
            }
        }

        public void SaveChanges()
        {
            using var transaction = _conn!.BeginTransaction();
            try
            {
                var cmd = _conn.CreateCommand();
                cmd.CommandText = "DELETE FROM rows WHERE project_id = @pid";
                cmd.Parameters.AddWithValue("@pid", _projectId);
                cmd.ExecuteNonQuery();

                foreach (var row in Rows) // Much cleaner!
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO rows (row_uuid, project_id, row_name) VALUES (@uuid, @pid, @name)";
                    cmd.Parameters.AddWithValue("@uuid", row.Uuid);
                    cmd.Parameters.AddWithValue("@pid", _projectId);
                    cmd.Parameters.AddWithValue("@name", row.Name);
                    cmd.ExecuteNonQuery();
                }
                cmd.Parameters.Clear();
                cmd.CommandText = "DELETE FROM seats WHERE project_id = @pid";
                cmd.Parameters.AddWithValue("@pid", _projectId);
                cmd.ExecuteNonQuery();

                foreach (var seat in Seats)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = @"
                        INSERT INTO seats (seat_uuid, project_id, row_uuid, seat_no, participant_id, device_record_uuid, position_x, position_y, angle)
                        VALUES (@suuid, @pid, @ruuid, @no, @partid, @devuuid, @x, @y, @a)
                    ";
                    cmd.Parameters.AddWithValue("@suuid", seat.SeatUuid);
                    cmd.Parameters.AddWithValue("@pid", _projectId);
                    cmd.Parameters.AddWithValue("@ruuid", string.IsNullOrEmpty(seat.RowUuid) ? (object)DBNull.Value : seat.RowUuid);
                    cmd.Parameters.AddWithValue("@no", int.TryParse(seat.Number, out int no) ? no : 0);
                    cmd.Parameters.AddWithValue("@partid", seat.Person?.ParticipantId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@devuuid", seat.Person?.Device?.DeviceUuid ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@x", seat.PositionX);
                    cmd.Parameters.AddWithValue("@y", seat.PositionY);
                    cmd.Parameters.AddWithValue("@a", seat.Angle);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void AddRowName(string name)
        {
            if (AppMode != AppMode.EditMode) return;
            if (!Rows.Any(r => r.Name == name))
            {
                Rows.Add(new Row { Uuid = Guid.NewGuid().ToString(), Name = name });
                SaveChanges();
            }
        }

        public void DeleteRow(string rowName)
        {
            if (AppMode != AppMode.EditMode) return;

            // 1. Find the actual Row object by its name.
            var rowToDelete = Rows.FirstOrDefault(r => r.Name == rowName);

            // 2. If it doesn't exist, we're done.
            if (rowToDelete == null) return;

            // 3. Check if any seats are still using this row's UUID.
            //    This check is now much safer and more reliable.
            if (Seats.Any(s => s.RowUuid == rowToDelete.Uuid))
            {
                // You should probably show this error to the user.
                throw new InvalidOperationException("Cannot delete a row that still has seats assigned.");
            }

            // 4. Remove the row object from the one and only list.
            Rows.Remove(rowToDelete);
            SaveChanges();
        }

        public void AddEmptySeat(double x, double y)
        {
            if (AppMode != AppMode.EditMode) return;
            if (Seats.Count >= 1000) throw new InvalidOperationException("超過1000個座位限制");
            var newSeat = new Seat { PositionX = x, PositionY = y };
            Seats.Add(newSeat);
            SaveChanges();
        }

        public void UpdateSeat(Seat seat, string rowName, string number, string personName, string deviceType, string deviceNumber)
        {
            if (AppMode != AppMode.EditMode) return;
            if (!string.IsNullOrEmpty(rowName) && !string.IsNullOrEmpty(number) &&
                Seats.Any(s => s != seat && s.RowName == rowName && s.Number == number))
                throw new InvalidOperationException("座位 (RowName, Number) 重複");

            var row = Rows.FirstOrDefault(r => r.Name == rowName);
            seat.RowName = row?.Name ?? "";
            seat.RowUuid = row?.Uuid ?? "";

            seat.Number = string.IsNullOrEmpty(number) ? null : number;
            seat.Person = string.IsNullOrEmpty(personName) ? null : new Person
            {
                Name = personName,
                Device = string.IsNullOrEmpty(deviceType) ? null : new Device { TypeUuid = deviceType, SpecificDataJson = deviceNumber }
            };
            SaveChanges();
        }

        public void AlignLeft()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            double minX = SelectedSeats.Min(s => s.PositionX);
            foreach (var seat in SelectedSeats)
                seat.PositionX = minX;
            SaveChanges();
        }

        public void AlignRight()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            double maxX = SelectedSeats.Max(s => s.PositionX);
            foreach (var seat in SelectedSeats)
                seat.PositionX = maxX;
            SaveChanges();
        }

        public void AlignTop()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            double minY = SelectedSeats.Min(s => s.PositionY);
            foreach (var seat in SelectedSeats)
                seat.PositionY = minY;
            SaveChanges();
        }

        public void AlignBottom()
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            double maxY = SelectedSeats.Max(s => s.PositionY);
            foreach (var seat in SelectedSeats)
                seat.PositionY = maxY;
            SaveChanges();
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
            SaveChanges();
        }

        public void ClearSelection()
        {
            if (AppMode != AppMode.EditMode) return;
            foreach (var seat in SelectedSeats)
                seat.IsSelected = false;
            SelectedSeats.Clear();
            SaveChanges();
        }

        public void RotateSelectedSeats(double delta)
        {
            if (AppMode != AppMode.EditMode || !SelectedSeats.Any()) return;
            foreach (var seat in SelectedSeats)
            {
                seat.Angle = (seat.Angle + delta) % 360;
            }
            SaveChanges();
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
            SaveChanges();
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
            SaveChanges();
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
            SaveChanges();
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
            SaveChanges();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}