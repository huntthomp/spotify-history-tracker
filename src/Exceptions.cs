using Auralytics.Models;

[Serializable]
public class UserTokenRefreshException : Exception
{
    public UserTokenRefreshException(Guid userId, Exception innerException)
        : base(userId.ToString(), innerException)
    { }
}
public class HistoryUpdateException : Exception
{
    public HistoryUpdateException(SpotifyUser user, Exception innerException)
        : base(user.user_id.ToString(), innerException)
    { }
}
public class SpotifyServiceException : Exception
{
    public string httpError;
    public SpotifyServiceException(string context, string httpError)
        : base(context)
    {
        this.httpError = httpError;
    }
}