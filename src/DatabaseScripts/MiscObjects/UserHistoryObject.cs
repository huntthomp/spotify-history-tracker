using Spotify.Models;

namespace Auralytics.Models
{
    //Default object to help process user streams.
    /*Lifecycle
    1) assign user and token data
    2) refresh user token data
    3) enrich with user history
    */
    public class UserHistoryObject
    {
        public required SpotifyUser SpotifyUser { get; set; }
        public required UserSpotifyToken UserSpotifyToken { get; set; }
        public RecentlyPlayedResponse? history { get; set; }
        public Exception? error { get; set; }
    }
}