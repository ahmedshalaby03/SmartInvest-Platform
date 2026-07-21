using SmartInvest.Domain.Common;

namespace SmartInvest.Domain.Interfaces;

/// <summary>
/// Coordinates repositories and commits all changes in a single transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
