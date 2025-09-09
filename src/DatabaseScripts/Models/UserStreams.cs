namespace Auralytics.Models
{
    public class UserStream
    {
        public required Guid user_id { get; set; }
        public required string track_id { get; set; }
        public required DateTimeOffset timestamp { get; set; }
        public required int ms_played { get; set; }
    }
}