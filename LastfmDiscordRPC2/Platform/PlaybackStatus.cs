namespace LastfmDiscordRPC2.Platform;

/// <summary>
/// Native playback state from OS or player (e.g. Windows SMTC, foobar2000).
/// Used to distinguish paused vs stopped when Last.fm does not report now playing.
/// </summary>
public enum PlaybackStatus
{
    /// <summary>Unknown or unavailable (e.g. non-Windows or API failed).</summary>
    Unknown,

    Playing,
    Paused,
    Stopped
}
