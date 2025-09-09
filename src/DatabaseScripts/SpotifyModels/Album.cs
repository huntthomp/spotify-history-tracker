namespace Spotify.Models
{
    public class Album
    {
        public required List<Artist> artists { get; set; }
        public required string id { get; set; }
        public required List<Image> images { get; set; }
        public required string name { get; set; }
        public required string release_date { get; set; }
        public required string release_date_precision { get; set; }
        public required int total_tracks { get; set; }
    }
}