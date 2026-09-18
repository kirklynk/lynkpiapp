namespace lynkpiapp
{
    public class InAppNotificationService : NotifyBase
    {

        public string? Message { get => field; private set => NotifyPropertyChanged(ref field, value); }

        public void PushMessage(string message)
        {
            Message = $"{message} {DateTime.Now.ToString("M/d/yyyy h:mm:ss tt")}";
            Message = string.Empty;
        }
    }
}
