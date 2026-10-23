namespace Identity.Application.Contracts;

// Returned by register and login. The client keeps the token and sends it as "Authorization: Bearer <token>".
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
