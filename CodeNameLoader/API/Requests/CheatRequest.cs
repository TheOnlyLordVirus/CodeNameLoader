using System;

namespace CodeNameLoader.API.Requests;

public enum CheatCommand
{
    GetCheat
}

public class CheatRequest
{
    public CheatCommand Command { get; set; }

    public Guid UserId { get; set; } = Guid.Empty;

    public string UserPassword { get; set; } = string.Empty;

    public Guid GameId { get; set; } = Guid.Empty;
}

