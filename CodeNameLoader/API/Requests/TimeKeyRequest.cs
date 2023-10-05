using System;

namespace CodeNameLoader.API.Requests;

public enum TimeKeyCommand
{
    Create,
    Redeem
}

public class TimeKeyRequest
{
    public TimeKeyCommand Command { get; set; }

    public Guid UserId { get; set; } = Guid.Empty;

    public string UserPassword { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

}
