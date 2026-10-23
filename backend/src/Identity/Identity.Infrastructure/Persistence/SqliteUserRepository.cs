using Identity.Application;
using Identity.Domain;

namespace Identity.Infrastructure.Persistence;

/// <summary>Adapter from ADO.NET to <see cref="IUserRepository"/>. The only class that knows the Users SQL.</summary>
public sealed class SqliteUserRepository : IUserRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteUserRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<bool> AddAsync(User user, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
