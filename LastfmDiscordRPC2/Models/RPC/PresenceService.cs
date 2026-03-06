using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.String;
using LastfmDiscordRPC2.DataTypes;
using LastfmDiscordRPC2.Exceptions;
using LastfmDiscordRPC2.IO;
using LastfmDiscordRPC2.Logging;
using LastfmDiscordRPC2.Models.API;
using LastfmDiscordRPC2.Models.Responses;
using LastfmDiscordRPC2.ViewModels;
using Newtonsoft.Json;

namespace LastfmDiscordRPC2.Models.RPC;

public sealed class PresenceService : IPresenceService
{
    private readonly LoggingService _loggingService;
    private readonly LastfmAPIService _lastfmService;
    private readonly IDiscordClient _discordClient;
    private readonly SaveCfgIOService _saveCfgService;
    private readonly UIContext _context;

    private readonly PeriodicTimer _timer;
    private CancellationTokenSource _timerCancellationTokenSource;

    private bool _isFirstSuccess;
    private int _exceptionCount;
    private bool _hasClearedDueToStopped;

    private bool IsRetry => _exceptionCount <= 3;
    private const int StoppedThresholdPolls = 1;
    private const int PollIntervalSeconds = 2;

    public PresenceService(
        LoggingService loggingService,
        LastfmAPIService lastfmService,
        IDiscordClient discordClient,
        SaveCfgIOService saveCfgService,
        UIContext context)
    {
        _loggingService = loggingService;
        _lastfmService = lastfmService;
        _discordClient = discordClient;
        _saveCfgService = saveCfgService;
        _context = context;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(PollIntervalSeconds));
        _timerCancellationTokenSource = new CancellationTokenSource();
    }

    public async Task SetPresence()
    {
        _timerCancellationTokenSource = new CancellationTokenSource();
        _exceptionCount = 0;
        SaveCfg saveSnapshot = _saveCfgService.GetSaveSnapshot();

        _isFirstSuccess = true;
        _hasClearedDueToStopped = false;
        _discordClient.Initialize(saveSnapshot);
        await PresenceLoop(saveSnapshot);
    }

    private async Task PresenceLoop(SaveCfg saveSnapshot)
    {
        int count = 0;
        try
        {
            while (await _timer.WaitForNextTickAsync(_timerCancellationTokenSource.Token).ConfigureAwait(false))
            {
                if (count++ < 3 && !_discordClient.IsReady)
                {
                    if (count == 3)
                    {
                        UnsetPresence();
                    }
                    continue;
                }
                count = 0;
                try
                {
                    TrackResponse response = await _lastfmService.GetRecentTracks(saveSnapshot.UserAccount.Username);
                    await UpdatePresence(response, saveSnapshot.UserRPCCfg.ExpiryMode).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    bool retry = HandleError(e);
                    if (retry)
                    {
                        _loggingService.Info($"Attempting to reconnect... Try {_exceptionCount}");
                    }
                    else
                    {
                        _timerCancellationTokenSource.Cancel();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _loggingService.Info("Presence has been expired.");
            ClearPresence();
        }
    }

    /// <summary>
    /// Returns true only when Last.fm explicitly reports nowplaying="true". Missing, empty, or "false" all count as stopped.
    /// </summary>
    private static bool IsNowPlaying(Track track)
    {
        string state = track.NowPlaying?.State?.Trim() ?? Empty;
        return state.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task UpdatePresence(TrackResponse response, bool deactivateWhenInactive)
    {
        if (response.RecentTracks.Tracks.Count == 0)
        {
            _loggingService.Info("No tracks found for user.");
            UnsetPresence();
            return;
        }

        Track firstTrack = response.RecentTracks.Tracks[0];
        bool isNowPlaying = IsNowPlaying(firstTrack);

        if (isNowPlaying)
        {
            if (_hasClearedDueToStopped)
            {
                _loggingService.Info($"Playback restarted. Now playing: {firstTrack.Artist.Name} - {firstTrack.Name}");
            }
            _hasClearedDueToStopped = false;
        }
        else
        {
            // Last.fm says not now playing: never use Windows API to show RPC. Default to Last.fm only; no RPC when Last.fm says not playing.
            if (_hasClearedDueToStopped)
            {
                return;
            }

            if (deactivateWhenInactive)
            {
                _loggingService.Info("Last.fm reports not playing. Cleared RPC.");
                _hasClearedDueToStopped = true;
                _discordClient.ClearPresence();
            }
            return;
        }

        if (_isFirstSuccess)
        {
            _loggingService.Info("Track successfully received! Attempting to connect to presence...");
        }

        if (_discordClient.IsReady)
        {
            _exceptionCount = 0;
            _discordClient.SetPresence(response);

            if (_isFirstSuccess)
            {
                _loggingService.Info("Playback started. Presence has been set!");
            }

            _isFirstSuccess = false;
            return;
        }

        _loggingService.Warning("Discord client not initialised. Please restart and use a valid ID.");
        UnsetPresence();
    }

    public void UnsetPresence()
    {
        _timerCancellationTokenSource.Cancel();
    }


    private void ClearPresence()
    {
        _context.IsRichPresenceActivated = false;
        _discordClient.ClearPresence();
    }

    private bool HandleError(Exception e)
    {
        switch (e)
        {
            case LastfmException exception:
            {
                _loggingService.Error($"Last.fm {exception.Message}");
                _exceptionCount++;
                return exception.ErrorCode is LastfmErrorCode.Temporary or LastfmErrorCode.OperationFail && IsRetry;
            }
            case HttpRequestException requestException:
            {
                _loggingService.Error($"HTTP {requestException.StatusCode ?? 0}: {requestException.Message}");
                _exceptionCount++;
                return IsRetry;
            }
            case JsonReaderException readerException:
            {
                _loggingService.Error($"Malformed JSON received\n{readerException.Message}");
                _exceptionCount++;
                return IsRetry;
            }
            default:
                _loggingService.Error($"Unhandled exception! Please report to developers\n{e.Message}\n{e.StackTrace ?? Empty}");
                return false;
        }
    }

    public void Dispose()
    {
        ClearPresence();
        _timer.Dispose();
        _timerCancellationTokenSource.Dispose();
    }
}