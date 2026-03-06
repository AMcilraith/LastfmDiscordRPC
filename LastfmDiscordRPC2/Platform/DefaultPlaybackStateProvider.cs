using System.Threading;
using System.Threading.Tasks;

namespace LastfmDiscordRPC2.Platform;

/// <summary>
/// Default provider when no OS/player integration is available (e.g. non-Windows).
/// Always returns <see cref="PlaybackStatus.Unknown"/> so presence logic falls back to Last.fm only.
/// </summary>
public sealed class DefaultPlaybackStateProvider : IPlaybackStateProvider
{
    public Task<PlaybackStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(PlaybackStatus.Unknown);
}
