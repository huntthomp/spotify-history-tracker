using System.ComponentModel.DataAnnotations;

namespace Auralytics.Models
{
    public class ServiceToken
    {
        [Key]
        public required string token_name { get; set; }
        public required DateTimeOffset last_refresh { get; set; }
        public required int duration { get; set; }
        public required string token { get; set; }
        public bool isExpired()
        {
            return last_refresh.AddSeconds(duration) <= DateTimeOffset.Now;
        }
    }
}
