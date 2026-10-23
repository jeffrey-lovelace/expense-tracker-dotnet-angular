namespace Identity.Application.Contracts;

// Never includes the password hash.
public sealed record UserResponse(Guid Id, string Email, string DisplayName);
