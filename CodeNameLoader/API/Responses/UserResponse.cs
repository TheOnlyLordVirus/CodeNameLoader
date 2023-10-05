using System;

namespace CodeNameLoader.API.Responses;

public sealed class UserResponse
{
    public string? Email { get; set; }

    public string? Name { get; set; }

    public bool? Admin { get; set; }

    public string? RegistrationIp { get; set; }

    public string? RecentIp { get; set; }

    public DateTime? CreationDate { get; set; }

    public bool? Active { get; set; }
}
