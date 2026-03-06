using System.Threading;
using System.Threading.Tasks;

namespace LastfmDiscordRPC2.Platform;

/// <summary>
/// Provides native playback state (playing/paused/stopped) from the OS or player,
/// e.g. Windows System Media Transport Controls or foobar2000 COM.
/// Returns <see cref="PlaybackStatus.Unknown"/> when not available (non-Windows or unsupported).
/// </summary>
public interface IPlaybackStateProvider
{
    /// <summary>
    /// Gets the current playback status. May be async for COM or WinRT.
    /// </summary>
    Task<PlaybackStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
