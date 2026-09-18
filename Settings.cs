namespace lynkpiapp
{
    public class Settings
    {
        public const string NAME = "Settings";
        public double Delay { get; set; } = 45.0;
        public List<Sensor> Sensors { get; set; } = [];

        public List<User> Users { get; set; } = [];
    }
}
