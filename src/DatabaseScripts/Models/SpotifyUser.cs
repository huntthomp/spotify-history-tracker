using System.ComponentModel.DataAnnotations;

namespace Auralytics.Models
{
    public class SpotifyUser
    {
        [Key]
        public required Guid user_id { get; set; }
        public DateTimeOffset? last_history_update { get; set; }
        public DateTimeOffset? initial_tracking_date { get; set; }
    }
}
