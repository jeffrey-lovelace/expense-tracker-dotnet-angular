using Microsoft.Data.Sqlite;

namespace Identity.Infrastructure.Persistence;

/// <summary>Creates a new, closed connection per operation. The provider pools the underlying handles.</summary>
public interface ISqliteConnectionFactory
{
    SqliteConnection Create();
}
