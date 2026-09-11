using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Device.Gpio;

namespace lynkpiapp
{
    public class HardwareService : NotifyBase, IDisposable
    {
        bool _disposed;
        private readonly List<Sensor>? _sensors;
        private readonly List<User>? _users;
        private readonly GpioController? _controller;
        private readonly List<GpioPin>? _pins;
        private readonly ILogger<HardwareService> _logger;

        public ObservableCollection<(string desc, int number, bool isTripped)> Tripped = new ObservableCollection<(string, int number, bool)>();

        ConcurrentStack<Sensor> _queue = new();

        public DateTime Clock { get => field; private set => NotifyPropertyChanged(ref field, value); }

        public bool IsTriggered { get => field; private set => NotifyPropertyChanged(ref field, value); } = false;

        public AlarmState AlarmState { get => field; private set => NotifyPropertyChanged(ref field, value); } = AlarmState.ArmedHome;

        public HardwareService(ILogger<HardwareService> logger, IOptions<List<Sensor>> sensors, IOptions<List<User>> users)
        {
            _logger = logger;

            try
            {
                ConfigureClockMonitoringService();

                _sensors = sensors?.Value ?? [];
                _users = users?.Value ?? [];
                _pins = [];

                _controller = new GpioController();

                foreach (var trigger in _sensors)
                {
                    var pin = _controller.OpenPin(trigger.PinNumber, PinMode.InputPullDown);
                    pin.ValueChanged += Pin_ValueChanged;
                    trigger.IsTriggered = pin.Read() == PinValue.Low;
                    _pins.Add(pin);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing GPIO controller.");
            }
        }

        private void Pin_ValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {

            var tripped = pinValueChangedEventArgs.ChangeType == PinEventTypes.Falling;
            var sensor = _sensors.FirstOrDefault(t => t.PinNumber == pinValueChangedEventArgs.PinNumber);
            sensor?.IsTriggered = tripped;

            if (sensor == null || AlarmState == AlarmState.Disarm || (AlarmState == AlarmState.ArmedHome && !sensor.IsPeripheral))
            {
                _logger.LogWarning("Ignoring raise alarm.");
                return;
            }
            _queue.Push(sensor);
        }

        private void RaiseAlarm(Sensor sensor)
        {
            if (!Tripped.Any(x => x.number == sensor.PinNumber))
                Tripped.Add((sensor.Description, sensor.PinNumber, true));
            _logger.LogWarning("Alarm triggered by sensor: {SensorDescription} at {Time}", sensor.Description, Clock);

            if (IsTriggered)
            {
                return;
            }

            IsTriggered = true;
        }

        private void ConfigureClockMonitoringService()
        {
            _ = Task.Factory.StartNew(() =>
            {
                while (!_disposed)
                {
                    Clock = DateTime.Now;
                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);

            _ = Task.Factory.StartNew(() =>
            {

                while (!_disposed)
                {
                    if (_queue.TryPop(out var sensor))
                    {
                        RaiseAlarm(sensor);
                    }
                    Thread.Sleep(500);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public List<(string description, bool isPeripheral, bool state)> CheckSensors()
        {
            var opened = new List<(string description, bool isPeripheral, bool state)>();
            foreach (var item in _sensors ?? [])
            {
                var pin = _pins?.FirstOrDefault(x => x.PinNumber == item.PinNumber);
                if (pin != null)
                {
                    opened.Add((item.Description, item.IsPeripheral, pin.Read() == PinValue.High));
                }
            }
            return opened;
        }
        public (bool isValid, string? user) SetAlarm(AlarmState alarmState, string code)
        {

            var (isValid, user) = ValidateCode(code);
            if (!isValid)
            {
                _logger.LogWarning("Failed to set alarm state due to invalid code.");
                return (false, null);
            }

            AlarmState = alarmState;
            if (alarmState == AlarmState.Disarm)
            {
                _queue.Clear();
                Tripped.Clear();
                IsTriggered = false;
            }
            return (true, user);
        }

        private (bool isValid, string? user) ValidateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Attempted to validate an empty or null code.");
                return (false, null);
            }

            var isValid = _users.FirstOrDefault(u => u.Code == code);
            if (isValid == null)
            {
                _logger.LogWarning("Invalid code attempted: {Code}", code);
            }
            return (isValid != null, isValid?.Name);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

        }
    }

    public enum AlarmState
    {
        ArmedHome,
        ArmedAway,
        Disarm,
    }
}
