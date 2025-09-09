namespace Spotify.Models
{
    public class Image
    {
        public required int height { get; set; }
        public required string url { get; set; }
        public required int width { get; set; }
    }
    public class Followers
    {
        public required int total { get; set; }
    }
    public class Context
    {
        public required string href { get; set; }
        public required string type { get; set; }
        public required string uri { get; set; }
    }
    public class DatabaseObjectCollection
    {
        public required Auralytics.Models.Track track { get; set; }
        public required Auralytics.Models.Album album { get; set; }
        public required Auralytics.Models.Artist[] artist { get; set; }
        public required Auralytics.Models.AlbumTrack albumTrack { get; set; }
        public required Auralytics.Models.ArtistAlbum[] artistAlbum { get; set; }
        public required Auralytics.Models.ArtistTrack[] artistTrack { get; set; }
    }
}