using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Device.Gpio;

namespace lynkpiapp
{
    public class HardwareService : NotifyBase, IDisposable
    {
        bool _disposed;
        private readonly Settings? _settings;
        private readonly GpioController? _controller;
        private readonly List<GpioPin>? _pins;
        private readonly ILogger<HardwareService> _logger;
        ConcurrentStack<Sensor> _queue = new();

        public ObservableCollection<(string desc, int number, bool isTripped)> Tripped { get; } = new ObservableCollection<(string, int number, bool)>();

        public DateTime Clock { get => field; private set => NotifyPropertyChanged(ref field, value); }
        public bool IsTriggered { get => field; private set => NotifyPropertyChanged(ref field, value); } = false;

        public TimeSpan CountDownTimer { get => field; private set => NotifyPropertyChanged(ref field, value); }
        public AlarmState PendingState { get => field; private set => NotifyPropertyChanged(ref field, value); }
        public AlarmState CurrentState { get => field; private set => NotifyPropertyChanged(ref field, value); } = AlarmState.ArmedAway;

        public HardwareService(ILogger<HardwareService> logger, IOptions<Settings> options)
        {
            _logger = logger;

            try
            {
                PendingState = CurrentState;

                ConfigureMonitoringService();
                _settings = options.Value;
                _pins = [];
                _controller = new GpioController();

                foreach (var trigger in _settings.Sensors)
                {
                    var pin = _controller.OpenPin(trigger.PinNumber, PinMode.InputPullDown);
                    pin.ValueChanged += Pin_ValueChanged;
                    trigger.IsTriggered = pin.Read() == PinValue.Low;
                    _pins.Add(pin);
                    if (trigger.IsTriggered)
                    {
                        Tripped.Add((trigger.Description, trigger.PinNumber, trigger.IsTriggered));
                    }
                }
                IsTriggered = Tripped.Count > 0;
                _logger.LogInformation("Found {0} tripped sensor", Tripped.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing GPIO controller.");
            }
        }

        public List<(string description, bool isPeripheral, bool state)> CheckSensors()
        {
            var opened = new List<(string description, bool isPeripheral, bool state)>();
            foreach (var item in _settings.Sensors)
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
            _logger.LogInformation("Setting alarm state");

            var (isValid, user) = ValidateCode(code);

            if (!isValid)
            {
                _logger.LogWarning("Failed to set alarm state due to invalid code.");
                return (false, null);
            }

            PendingState = alarmState;

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

            var isValid = _settings.Users.FirstOrDefault(u => u.Code == code);
            if (isValid == null)
            {
                _logger.LogWarning("Invalid code attempted: {Code}", code);
            }
            return (isValid != null, isValid?.Name);
        }

        private void ConfigureMonitoringService()
        {
            _ = Task.Factory.StartNew(() =>
            {
                while (!_disposed)
                {
                    Clock = DateTime.Now;
                    Thread.Sleep(1000);

                    if (PendingState == CurrentState && CountDownTimer == TimeSpan.FromSeconds(_settings!.Delay))
                    {
                        continue;
                    }

                    if (PendingState == AlarmState.Disarm)
                    {
                        CurrentState = PendingState;
                        CountDownTimer = TimeSpan.FromSeconds(_settings!.Delay);
                    }
                    else
                    {
                        CountDownTimer = CountDownTimer.Add(TimeSpan.FromSeconds(-1.0));
                        _logger.LogInformation($"Current count down {CountDownTimer.Seconds}");
                        if (CountDownTimer.Seconds == 0 || CountDownTimer.Seconds < 0)
                        {
                            CurrentState = PendingState;
                            CountDownTimer = TimeSpan.FromSeconds(_settings!.Delay);
                        }
                    }
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
        private void Pin_ValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {

            var tripped = pinValueChangedEventArgs.ChangeType == PinEventTypes.Falling;
            var sensor = _settings!.Sensors.FirstOrDefault(t => t.PinNumber == pinValueChangedEventArgs.PinNumber);
            sensor?.IsTriggered = tripped;

            if (sensor == null || CurrentState == AlarmState.Disarm)
            {
                _logger.LogWarning("Ignoring raise alarm.");
                return;
            }
            _queue.Push(sensor);
        }

        private void RaiseAlarm(Sensor sensor)
        {
            _logger.LogInformation("Checking whether to raised alarm");

            //Ignoring peripheral sensors when in ArmHome mode
            if (CurrentState == AlarmState.ArmedHome && !sensor.IsPeripheral)
            {
                return;
            }

            if (!Tripped.Any(x => x.number == sensor.PinNumber))
                Tripped.Add((sensor.Description, sensor.PinNumber, true));

            _logger.LogWarning("Alarm triggered by sensor: {SensorDescription} at {Time}", sensor.Description, Clock);

            if (IsTriggered)
            {
                return;
            }

            IsTriggered = true;
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
