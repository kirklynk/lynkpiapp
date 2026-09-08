using System.Device.Gpio;

namespace lynkpiapp
{
    public class HardwareService : NotifyBase, IDisposable
    {
        bool _disposed;
        private readonly List<TriggerInput> _triggers;
        private readonly GpioController _controller;
        private readonly List<GpioPin> _pins;
        private readonly ILogger<HardwareService> _logger;

        public DateTime Clock
        {
            get => field; set
            {
                NotifyPropertyChanged(ref field, value);
            }
        }

        public bool IsTriggered
        {
            get => field; protected set
            {
                NotifyPropertyChanged(ref field, value);
            }
        } = false;

        public AlarmState AlarmState { get => field; private set => NotifyPropertyChanged(ref field, value); } = AlarmState.ArmedHome;

        public HardwareService(ILogger<HardwareService> logger, IConfiguration configuration)
        {
            _logger = logger;

            try
            {
                SetupClock();
                _triggers = configuration.GetSection("TriggerInputs").Get<List<TriggerInput>>() ?? new List<TriggerInput>();
                _pins = new List<GpioPin>();

                _controller = new GpioController();

                foreach (var trigger in _triggers)
                {
                    var pin = _controller.OpenPin(trigger.PinNumber, PinMode.InputPullDown);
                    pin.ValueChanged += Pin_ValueChanged;
                    _pins.Add(pin);
                }

                _logger.LogInformation("GPIO controller initialized successfully.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing GPIO controller.");
            }
        }

        private void Pin_ValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            IsTriggered = pinValueChangedEventArgs.ChangeType == PinEventTypes.Falling;
            var triggerInput = _triggers.FirstOrDefault(t => t.PinNumber == pinValueChangedEventArgs.PinNumber);
            triggerInput?.IsTriggered = IsTriggered;
            _logger.LogInformation("Pin value changed: {ChangeType}, IsTriggered: {IsTriggered}", pinValueChangedEventArgs.ChangeType, IsTriggered);
        }

        private void SetupClock()
        {
            _ = Task.Factory.StartNew(() =>
            {
                while (!_disposed)
                {
                    Clock = DateTime.Now;
                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public bool SetAlarm(AlarmState alarmState)
        {
            AlarmState = alarmState;
            return true;
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
