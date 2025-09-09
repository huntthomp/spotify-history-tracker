namespace Spotify.Models
{
    public class Artist
    {
        public required string id { get; set; }
        public required string name { get; set; }
    }
    public class ArtistLong
    {
        public required Followers followers { get; set; }
        public required string[] genres { get; set; }
        public required string id { get; set; }
        public required List<Image> images { get; set; }
        public required string name { get; set; }
        public required int popularity { get; set; }
    }
    public class SeveralArtistResponse
    {
        public required List<ArtistLong> artists { get; set; }
    }
}