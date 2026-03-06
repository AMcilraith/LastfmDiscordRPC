using System.Runtime.InteropServices;
using Windows.Media.Control;

namespace LastfmDiscordRPC2.Platform.Windows;

/// <summary>
/// Uses Windows System Media Transport Controls (SMTC) and optionally foobar2000 COM
/// to determine if playback is playing, paused, or stopped.
/// </summary>
public sealed class WindowsPlaybackStateProvider : IPlaybackStateProvider
{
    public async Task<PlaybackStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        PlaybackStatus fromSmtic = await GetStatusFromWindowsSmticAsync().ConfigureAwait(false);
        if (fromSmtic != PlaybackStatus.Unknown)
            return fromSmtic;

        return GetStatusFromFoobarCom();
    }

    private static async Task<PlaybackStatus> GetStatusFromWindowsSmticAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var session = manager.GetCurrentSession();
            if (session == null)
                return PlaybackStatus.Unknown;

            var playbackInfo = session.GetPlaybackInfo();
            if (playbackInfo == null)
                return PlaybackStatus.Unknown;

            // GlobalSystemMediaTransportControlsSessionPlaybackStatus: Closed=0, Opened=1, Changing=2, Stopped=3, Playing=4, Paused=5
            int status = (int)playbackInfo.PlaybackStatus;
            if (status == 4) return PlaybackStatus.Playing;
            if (status == 5) return PlaybackStatus.Paused;
            if (status == 3) return PlaybackStatus.Stopped;
            return PlaybackStatus.Unknown;
        }
        catch
        {
            return PlaybackStatus.Unknown;
        }
    }

    private static PlaybackStatus GetStatusFromFoobarCom()
    {
        try
        {
            Type? foobarType = Type.GetTypeFromProgID("Foobar2000.Application.0.7");
            if (foobarType == null)
                return PlaybackStatus.Unknown;

            object? app = Activator.CreateInstance(foobarType);
            if (app == null)
                return PlaybackStatus.Unknown;

            try
            {
                object? playback = foobarType.InvokeMember("Playback", System.Reflection.BindingFlags.GetProperty, null, app, null);
                if (playback == null)
                    return PlaybackStatus.Unknown;

                Type playbackType = playback.GetType();
                object? isPaused = playbackType.InvokeMember("IsPaused", System.Reflection.BindingFlags.GetProperty, null, playback, null);
                object? isPlaying = playbackType.InvokeMember("IsPlaying", System.Reflection.BindingFlags.GetProperty, null, playback, null);

                // COM variant: -1 (true) or 0 (false)
                bool paused = isPaused is int p && p != 0;
                bool playing = isPlaying is int q && q != 0;

                if (paused) return PlaybackStatus.Paused;
                if (playing) return PlaybackStatus.Playing;
                return PlaybackStatus.Stopped;
            }
            finally
            {
                if (app is IDisposable d)
                    d.Dispose();
                else if (Marshal.IsComObject(app))
                    Marshal.ReleaseComObject(app);
            }
        }
        catch
        {
            return PlaybackStatus.Unknown;
        }
    }
}
