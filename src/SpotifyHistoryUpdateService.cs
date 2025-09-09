using Auralytics.Data;
using Auralytics.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NCrontab;
using Spotify.Models;

public class SpotifyHistoryUpdateService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SpotifyHistoryUpdateService> _logger;
    private readonly ISpotifyTokenRefresher _tokenRefresher;
    private readonly ISpotifyAPIInterface _spotifyAPIInterface;
    private readonly IConfiguration _configuartion;
    private Timer? _timer;
    private CrontabSchedule _schedule;
    private DateTime _nextRunTime;

    public SpotifyHistoryUpdateService(IConfiguration configuration, ISpotifyTokenRefresher tokenRefresher, ISpotifyAPIInterface spotifyAPIInterface, ILogger<SpotifyHistoryUpdateService> logger, IServiceScopeFactory scopeFactory)
    {
        _configuartion = configuration;
        _tokenRefresher = tokenRefresher;
        _spotifyAPIInterface = spotifyAPIInterface;
        _logger = logger;
        _scopeFactory = scopeFactory;
        // Fallback if error on first container run
        _nextRunTime = DateTime.Now;

        string cronExpression = "0,30 * * * *";
        _schedule = CrontabSchedule.Parse(cronExpression);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = ExecuteTask();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private void ScheduleNextTask(TimeSpan? overrideNextRunTime = null)
    {
        DateTime currentTime = DateTime.Now;

        _nextRunTime = overrideNextRunTime == null ? _schedule.GetNextOccurrence(currentTime) : _nextRunTime.Add((TimeSpan)overrideNextRunTime);

        TimeSpan timeUntilNextRun = _nextRunTime - currentTime;

        _logger.LogInformation($"\x1b[32mNext history update task will execute at: {_nextRunTime}\x1b[0m");

        _timer = new Timer(async state => await ExecuteTask(), null, timeUntilNextRun, Timeout.InfiniteTimeSpan);
    }

    private async Task ExecuteTask()
    {
        IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            List<UserHistoryObject> users = await (from user in context.spotify_users
                                                   join token in context.user_spotify_tokens
                                                   on user.user_id equals token.user_id
                                                   select new UserHistoryObject
                                                   {
                                                       SpotifyUser = user,
                                                       UserSpotifyToken = token
                                                   }).ToListAsync();
            await updateUserHistory(users);
            List<(Guid, Exception)> failedUsers = users.Where(x => x.error != null).Select(x => (x.SpotifyUser.user_id, x.error!)).ToList();
            await TelegramBot.sendUserErrorMessage(failedUsers);
            if (failedUsers.Count == 0)
            {
                ScheduleNextTask();
            }
            else
            {
                ScheduleNextTask(TimeSpan.FromMinutes(5));
            }
        }
        catch (SpotifyServiceException e)
        {
            _logger.LogError($"Error with spotify service {e}");
            await TelegramBot.sendUnexpectedHttpErrorMessage(e);
            ScheduleNextTask(TimeSpan.FromMinutes(5));
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error ocurred {e}");
            await TelegramBot.sendUnexpectedErrorMessage(e);
            ScheduleNextTask(TimeSpan.FromMinutes(5));
        }
    }
    public async Task updateUserHistory(List<UserHistoryObject> users)
    {
        IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Enriches users with updated tokens
        await _tokenRefresher.RefreshTokens(users);

        // Save changes, at this point only tokens have been changed
        await context.SaveChangesAsync();

        HashSet<Spotify.Models.Track> tracks = new HashSet<Spotify.Models.Track>();
        List<UserStream> userStreams = new List<UserStream>();
        // Enrich users with history object
        foreach (UserHistoryObject user in users)
        {
            if (user.error != null) continue;
            // Updates history and returns all streamed tracks
            HashSet<Spotify.Models.Track> streamedTracks = await _spotifyAPIInterface.populateUserHistory(user);
            userStreams.AddRange(user.history!.items.Select(x => new UserStream
            {
                user_id = user.SpotifyUser.user_id,
                track_id = x.track.id,
                timestamp = x.played_at.UtcDateTime,
                ms_played = x.track.duration_ms
            }));
            tracks.UnionWith(streamedTracks);

            if (user.SpotifyUser.user_id.ToString() == "d7247f2e-7775-42dd-a01f-8963f9460bd9")
            {
                await TelegramBot.SendHistoryUpdateMessage(user.history);
            }
        }
        // Insert database relations
        await addAndUpdateDatabaseRelations(tracks);

        _logger.LogInformation($"\x1b[35m{userStreams.Count} new streams\x1b[0m");
        // Add user streams to DB
        await context.BulkInsertOrUpdateAsync(userStreams);

        // Itentionally update cursors last
        foreach (UserHistoryObject user in users)
        {
            // Don't update a user's history if an error ocurred
            if (user.history?.cursors == null) continue;
            DateTimeOffset afterCursor = DateTimeOffset.FromUnixTimeMilliseconds(user.history.cursors.after).UtcDateTime;
            user.SpotifyUser.initial_tracking_date ??= afterCursor;
            user.SpotifyUser.last_history_update = afterCursor;
            context.Entry(user.SpotifyUser).State = EntityState.Modified;
        }

        await context.SaveChangesAsync();
    }
    public async Task addAndUpdateDatabaseRelations(HashSet<Spotify.Models.Track> tracks)
    {
        IServiceScope scope = _scopeFactory.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Map types
        List<Auralytics.Models.Track> trackList = new List<Auralytics.Models.Track>();
        List<Auralytics.Models.Album> albumList = new List<Auralytics.Models.Album>();
        List<Auralytics.Models.Artist> artistList = new List<Auralytics.Models.Artist>();
        List<Auralytics.Models.AlbumTrack> albumTrackList = new List<Auralytics.Models.AlbumTrack>();
        List<Auralytics.Models.ArtistAlbum> artistAlbumList = new List<Auralytics.Models.ArtistAlbum>();
        List<Auralytics.Models.ArtistTrack> artistTrackList = new List<Auralytics.Models.ArtistTrack>();

        foreach (Spotify.Models.Track track in tracks)
        {
            DatabaseObjectCollection collection = track.getDatabaseRelations();
            trackList.Add(collection.track);
            albumList.Add(collection.album);
            artistList.AddRange(collection.artist);
            albumTrackList.Add(collection.albumTrack);
            artistAlbumList.AddRange(collection.artistAlbum);
            artistTrackList.AddRange(collection.artistTrack);
        }
        artistList = artistList.DistinctBy(x => x.id).ToList();
        albumList = albumList.DistinctBy(x => x.id).ToList();

        // Remove all old artists
        List<Auralytics.Models.Artist> oldArtists = await context.artists.Where(row => artistList.Select(artist => artist.id).Contains(row.id)).ToListAsync();
        HashSet<string> oldArtistIds = oldArtists.Select(a => a.id).ToHashSet();
        artistList.RemoveAll(x => oldArtistIds.Contains(x.id));

        _logger.LogInformation($"\x1b[34m{artistList.Count} new artists\x1b[0m");

        // Enrich artist with properties
        await _spotifyAPIInterface.populateArtistInfo(artistList);

        await context.BulkInsertOrUpdateAsync(trackList);
        await context.BulkInsertOrUpdateAsync(albumList);
        await context.BulkInsertOrUpdateAsync(artistList);

        await context.BulkInsertOrUpdateAsync(albumTrackList);
        await context.BulkInsertOrUpdateAsync(artistAlbumList);
        await context.BulkInsertOrUpdateAsync(artistTrackList);
    }
}
