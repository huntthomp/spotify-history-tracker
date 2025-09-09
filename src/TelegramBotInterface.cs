using Spotify.Models;

static class TelegramBot
{
    static IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
    private static readonly string errorLoggerToken = configuration["Telegram:ErrorLoggerToken"]!;
    private static readonly string historyUpdateToken = configuration["Telegram:HistoryUpdateToken"]!;
    private static readonly long chatId = long.Parse(configuration["Telegram:ChatID"]!);
    public static async Task sendUnexpectedHttpErrorMessage(SpotifyServiceException exception)
    {
        await SendMessageError($"Unexpected HTTP error\n{exception.Message}\n\n{exception.httpError}");
    }
    public static async Task sendUnexpectedErrorMessage(Exception exception)
    {
        await SendMessageError($"{exception.Message}\n\n{exception.StackTrace}");
    }
    public static async Task sendUserErrorMessage(List<(Guid, Exception)> users)
    {
        string message = "";

        foreach ((Guid user_id, Exception exception) in users)
        {
            if (exception is SpotifyServiceException spotifyException)
            {
                message += $"{user_id}: {spotifyException.Message}\n{spotifyException.httpError}\n\n\n";
            }
            else
            {
                message += $"{user_id}: {exception.Message}\n{exception.StackTrace}\n\n\n";
            }
        }
        await SendMessageError(message);
    }
    public static async Task SendHistoryUpdateMessage(RecentlyPlayedResponse user)
    {
        int artistCount = user.items.SelectMany(x => x.track.artists).DistinctBy(x => x.id).Count();
        int albumCount = user.items.Select(x => x.track.album).DistinctBy(x => x.id).Count(); ;

        string message = $@"
        History Update
        New streams: {user.items.Count}
        Artist count: {artistCount}
        Album count: {albumCount}";
        await SendMessageHistory(message);
    }
    public static async Task SendMessageError(string message)
    {
        using (var client = new HttpClient())
        {
            var url = $"https://api.telegram.org/bot{errorLoggerToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}";

            HttpResponseMessage response = await client.GetAsync(url);
        }
    }
    public static async Task SendMessageHistory(string message)
    {
        using (var client = new HttpClient())
        {
            var url = $"https://api.telegram.org/bot{historyUpdateToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}";

            HttpResponseMessage response = await client.GetAsync(url);
        }
    }
}
