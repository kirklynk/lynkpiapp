namespace lynkpiapp
{
    public sealed class TriggerInput : NotifyBase
    {

        public bool IsTriggered
        {
            get => field;
            set => NotifyPropertyChanged(ref field, value);
        }

        public int PinNumber
        {
            get => field;
            set => NotifyPropertyChanged(ref field, value);
        }

        public string Description
        {
            get => field;
            set => NotifyPropertyChanged(ref field, value);
        }
    }
}
