using System.ComponentModel;
using System.Device.Gpio;
using System.Runtime.CompilerServices;

namespace lynkpiapp
{
    public class HardwareService : INotifyPropertyChanged, IDisposable
    {
        bool _disposed;
        private readonly GpioController _controller;
        private readonly GpioPin _pin;
        private readonly ILogger<HardwareService> _logger;

        public DateTime Clock
        {
            get => field; set
            {
                OnPropertyChanged(ref field, value);
            }
        }

        public bool IsTriggered
        {
            get => field; protected set
            {
                OnPropertyChanged(ref field, value);
            }
        } = false;

        public HardwareService(ILogger<HardwareService> logger)
        {
            _logger = logger;

            try
            {
                SetupClock();

                _controller = new GpioController();
                _pin = _controller.OpenPin(4, PinMode.InputPullDown);
                
                IsTriggered = _pin.Read() == PinValue.Low;

                _pin.ValueChanged += Pin_ValueChanged;  
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

        }

        void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
