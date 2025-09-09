using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auralytics.Data;
using Auralytics.Models;
using Microsoft.EntityFrameworkCore;
using Spotify.Models;
public interface ISpotifyAPIInterface
{

    Task<HashSet<Spotify.Models.Track>> populateUserHistory(UserHistoryObject user);
    Task populateArtistInfo(List<Auralytics.Models.Artist> artists);
}
public class SpotifyAPIInterface : IHostedService, ISpotifyAPIInterface
{
    public ServiceToken? clientCredentialsToken;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SpotifyAPIInterface> _logger;
    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;
    private static readonly HttpClient client = new HttpClient();
    public SpotifyAPIInterface(ILogger<SpotifyAPIInterface> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SpotifyAPIInterface...");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = Task.Run(() => RunBackgroundLoopAsync(_cts.Token));

        return Task.CompletedTask;
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SpotifyAPIInterface...");
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
    private async Task ensureValidClientCredentialsToken()
    {
        IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Database handles refresh, so update local instance on expiration
        if (clientCredentialsToken == null || clientCredentialsToken.isExpired())
        {
            clientCredentialsToken = await context.service_tokens.Where(x => x.token_name == "client_credentials").FirstAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientCredentialsToken.token);
        }
    }
    public async Task populateArtistInfo(List<Auralytics.Models.Artist> artists)
    {
        await ensureValidClientCredentialsToken();

        foreach (var artistChunk in artists.Chunk(50))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/artists?ids={string.Join(",", artistChunk.Select(x => x.id))}");
            HttpResponseMessage responseRaw = await client.SendAsync(request);
            if (responseRaw.StatusCode != HttpStatusCode.OK)
            {
                throw new SpotifyServiceException($"Error when getting new artists {responseRaw.StatusCode}", await responseRaw.Content.ReadAsStringAsync());
            }
            SeveralArtistResponse response = (await responseRaw.Content.ReadFromJsonAsync<SeveralArtistResponse>())!;
            // Fill properties, should maintain order
            for (int i = 0; i < artistChunk.Length; i++)
            {
                Auralytics.Models.Artist baseArtist = artistChunk[i];
                ArtistLong fullArtist = response.artists[i];
                // Throw exception if order is not retained or id mismatch for some reason
                if (baseArtist.id != fullArtist.id) throw new Exception($"Artist out of order during population. {artistChunk[i].id} != {response.artists[i].id}");
                if (fullArtist.images.Count != 0)
                {
                    baseArtist.cover = fullArtist.images[0].url;
                }
                baseArtist.followers = fullArtist.followers.total;
            }
        }
    }
    public async Task<HashSet<Spotify.Models.Track>> populateUserHistory(UserHistoryObject user)
    {
        try
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.UserSpotifyToken.access);
            long after = user.SpotifyUser.last_history_update != null ? ((DateTimeOffset)user.SpotifyUser.last_history_update).ToUnixTimeMilliseconds() : 0;
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/me/player/recently-played?limit=50&after={after}");
            HttpResponseMessage responseRaw = await client.SendAsync(request);
            if (responseRaw.StatusCode != HttpStatusCode.OK)
            {
                throw new SpotifyServiceException($"Error when getting user streams {responseRaw.StatusCode}", await responseRaw.Content.ReadAsStringAsync());
            }
            RecentlyPlayedResponse response = (await responseRaw.Content.ReadFromJsonAsync<RecentlyPlayedResponse>())!;
            user.history = response;
            return user.history.items.Select(x => x.track).ToHashSet();
        }
        catch (Exception e)
        {
            user.error = e;
            return [];
        }
    }
}