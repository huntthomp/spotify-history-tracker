using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Auralytics.Models;
using Spotify.Models;

public interface ISpotifyTokenRefresher
{
    Task RefreshTokens(List<UserHistoryObject> users);
}
public class SpotifyTokenRefreshService : IHostedService, ISpotifyTokenRefresher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SpotifyTokenRefreshService> _logger;
    private readonly IConfiguration _configuartion;
    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;
    private HttpClient client;

    public SpotifyTokenRefreshService(IConfiguration configuration, ILogger<SpotifyTokenRefreshService> logger, IServiceScopeFactory scopeFactory)
    {
        _configuartion = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;

        client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration["Spotify:ClientID"]}:{configuration["Spotify:ClientSecret"]}")));
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = Task.Run(() => RunBackgroundLoopAsync(_cts.Token));

        return Task.CompletedTask;
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts!.Cancel();

        if (_backgroundTask != null)
        {
            await _backgroundTask;
        }
    }
    private async Task RunBackgroundLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {

        }
        await Task.CompletedTask;
    }
    public async Task RefreshTokens(List<UserHistoryObject> users)
    {
        foreach (UserHistoryObject user in users)
        {
            try
            {
                await refreshUserToken(user);
            }
            catch (Exception e)
            {
                user.error = e;
            }
        }
    }
    public async Task refreshUserToken(UserHistoryObject user)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", user.UserSpotifyToken.refresh },
            })
        };
        HttpResponseMessage response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new SpotifyServiceException($"Error when refreshing user token {response.StatusCode}", await response.Content.ReadAsStringAsync());
        }
        RefreshToken newToken = (await response.Content.ReadFromJsonAsync<RefreshToken>())!;
        newToken.setExpiration();
        user.UserSpotifyToken.setNewInfo(newToken);
    }
}
