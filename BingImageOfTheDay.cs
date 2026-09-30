namespace lynkpiapp
{

    public class BingImageOfTheDay
    {
        public List<BingImage> Images { get; set; } = [];
    }

    public class BingImage
    {
        public string? Url { get; set; }
        public string? Copyright { get; set; }
        public string? Copyrightlink { get; set; }
        public string? Title { get; set; }
    }

}
