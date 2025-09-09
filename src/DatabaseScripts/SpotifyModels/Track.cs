namespace Spotify.Models
{
    public class Track
    {
        public required Album album { get; set; }
        public required List<Artist> artists { get; set; }
        public required int disc_number { get; set; }
        public required int duration_ms { get; set; }
        public required string id { get; set; }
        public required string name { get; set; }
        public required int track_number { get; set; }
        public override bool Equals(object? obj)
        {
            return obj is Track other && id == other.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
        public DatabaseObjectCollection getDatabaseRelations()
        {
            DateTimeOffset? albumReleaseDate = DateTimeOffset.TryParse(album.release_date, out var dto) ? dto.UtcDateTime : null;
            IEnumerable<Artist> allArtists = artists.Concat(album.artists).DistinctBy(x => x.id);
            return new DatabaseObjectCollection
            {
                track = new Auralytics.Models.Track
                {
                    id = id,
                    name = name,
                    duration = duration_ms,
                },
                album = new Auralytics.Models.Album
                {
                    id = album.id,
                    name = album.name,
                    cover = album.images[0].url,
                    release_date = albumReleaseDate,
                },
                artist = allArtists.Select(x => new Auralytics.Models.Artist
                {
                    id = x.id,
                    name = x.name,
                }).ToArray(),
                albumTrack = new Auralytics.Models.AlbumTrack
                {
                    album = album.id,
                    track = id,
                },
                artistAlbum = album.artists.Select(x => new Auralytics.Models.ArtistAlbum
                {
                    artist = x.id,
                    album = album.id,
                }).ToArray(),
                artistTrack = artists.Select(x => new Auralytics.Models.ArtistTrack
                {
                    artist = x.id,
                    track = id,
                }).ToArray()
            };
        }
    }
}