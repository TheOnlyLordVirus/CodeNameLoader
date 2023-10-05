using System;

namespace CodeNameLoader.API.Requests;

public enum UserCommand
{
    Create,
    Authenticate
}

public sealed class UserRequest
{
    public UserCommand UserCommand { get; set; }

    public string Email { get; set; }

    public string Name { get; set; }

    public string Password { get; set; }

    public bool Admin { get; set; } = false;

    public string RegistrationIp { get; set; }

    public string? RecentIp { get; set; }

    public DateTime CreationDate { get; set; } = DateTime.Now;

    public string HardwareId { get; set; }

    public bool Active { get; set; } = true;
}
