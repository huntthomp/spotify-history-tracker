namespace Auralytics.Models
{
    public class Track
    {
        public required string id { get; set; }
        public required string name { get; set; }

        public string? preview { get; set; }

        public required int duration { get; set; }
    }
}
