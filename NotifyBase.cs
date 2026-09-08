using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace lynkpiapp
{
    public class NotifyBase : INotifyPropertyChanged
    {

        protected void NotifyPropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
