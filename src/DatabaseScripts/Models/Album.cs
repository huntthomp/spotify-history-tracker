namespace Auralytics.Models
{
    public class Album
    {
        public required string id { get; set; }
        public required string name { get; set; }

        public string? cover { get; set; }

        public DateTimeOffset? release_date { get; set; }
    }
}