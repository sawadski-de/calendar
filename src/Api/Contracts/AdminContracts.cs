using Domain;

namespace Api.Contracts;

// `required` properties (rather than positional parameters) so System.Text.Json rejects a request
// body that omits `role` outright — a missing enum property would otherwise silently bind to its
// default value, PersonRole.Admin (0), granting unintended admin rights.
public record CreatePersonRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required PersonRole Role { get; init; }
}

public record ChangeRoleRequest
{
    public required PersonRole Role { get; init; }
}
