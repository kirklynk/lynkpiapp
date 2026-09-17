namespace lynkpiapp
{
    public class Settings
    {
        public const string NAME = "Settings";
        public List<Sensor> Sensors { get; set; } = [];

        public List<User> Users { get; set; } = [];
    }
}
