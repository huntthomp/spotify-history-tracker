namespace Spotify.Models
{
    public class RefreshToken
    {
        public required string access_token { get; set; }
        public required string token_type { get; set; }
        public required int expires_in { get; set; }
        public DateTimeOffset expires_at { get; private set; }
        public void setExpiration()
        {
            expires_at = DateTimeOffset.UtcNow.AddSeconds(expires_in);
        }
    }

}