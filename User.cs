namespace lynkpiapp
{
    public class User : NotifyBase
    {
        public string? Name { get => field; set => NotifyPropertyChanged(ref field, value); }
        public string? Code { get => field; set => NotifyPropertyChanged(ref field, value); }
    }
}
