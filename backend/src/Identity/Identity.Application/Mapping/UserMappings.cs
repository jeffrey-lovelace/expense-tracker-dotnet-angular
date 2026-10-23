using Identity.Application.Contracts;
using Identity.Domain;

namespace Identity.Application.Mapping;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserResponse(user.Id, user.Email, user.DisplayName);
    }
}
