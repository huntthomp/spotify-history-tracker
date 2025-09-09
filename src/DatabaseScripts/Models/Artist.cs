namespace Auralytics.Models
{
    public class Artist
    {
        public required string id { get; set; }
        public required string name { get; set; }
        public int followers { get; set; }
        public string? cover { get; set; }
    }
}