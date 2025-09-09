using Microsoft.EntityFrameworkCore;
using Auralytics.Models;

namespace Auralytics.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        // "public" schema
        public DbSet<Track> tracks { get; set; }
        public DbSet<Album> albums { get; set; }
        public DbSet<Artist> artists { get; set; }
        public DbSet<AlbumTrack> album_track { get; set; }
        public DbSet<ArtistAlbum> artist_album { get; set; }
        public DbSet<ArtistTrack> artist_track { get; set; }
        public DbSet<UserSpotifyToken> user_spotify_tokens { get; set; }
        public DbSet<SpotifyUser> spotify_users { get; set; }
        public DbSet<UserStream> user_streams_private { get; set; }
        // "spotify_data" schema
        public DbSet<ServiceToken> service_tokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define composite primary keys
            modelBuilder.Entity<AlbumTrack>()
                .HasKey(o => new { o.album, o.track });

            modelBuilder.Entity<ArtistAlbum>()
                .HasKey(o => new { o.artist, o.album });

            modelBuilder.Entity<ArtistTrack>()
                .HasKey(o => new { o.artist, o.track });

            modelBuilder.Entity<UserStream>()
                .HasKey(o => new { o.user_id, o.track_id, o.timestamp });

            // Configure tables for non-public schema tables
            modelBuilder.Entity<ServiceToken>()
                .ToTable("service_tokens", schema: "spotify_data");
        }
    }
}
