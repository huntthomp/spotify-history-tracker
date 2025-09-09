using System.ComponentModel.DataAnnotations;
using Spotify.Models;

namespace Auralytics.Models
{
    public class UserSpotifyToken
    {
        [Key]
        public required Guid user_id { get; set; }
        public required string access { get; set; }
        public required string refresh { get; set; }
        public DateTimeOffset expires_at { get; set; }

        public void setNewInfo(RefreshToken token)
        {
            access = token.access_token;
            expires_at = token.expires_at;
        }
    }
}
