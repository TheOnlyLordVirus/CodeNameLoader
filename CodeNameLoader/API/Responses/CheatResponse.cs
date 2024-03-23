using System;

namespace CodeNameLoader.API.Responses;

public class CheatResponse
{
    public Guid GameId { get; set; } = Guid.Empty;

    public string GameProcessName { get; set; } = string.Empty;

    public string GameName { get; set; } = string.Empty;

    public string GameVersion { get; set; } = string.Empty;

    public int CheatBinaryId { get; set; }

    public string CheatBinary { get; set; } = string.Empty;
}
