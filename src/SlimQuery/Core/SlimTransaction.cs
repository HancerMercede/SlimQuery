using System.Data;

namespace SlimQuery.Core;

public class SlimTransaction : IAsyncDisposable
{
    private readonly IDbTransaction _transaction;
    private bool _disposed;
    private bool _committed;

    public SlimTransaction(IDbTransaction transaction)
    {
        _transaction = transaction;
    }

    public IDbTransaction Transaction => _transaction;
    public bool IsCommitted => _committed;
    public IsolationLevel IsolationLevel => _transaction.IsolationLevel;

    public Task CommitAsync()
    {
        if (_disposed)
            throw new InvalidOperationException("Transaction already disposed");
        
        _transaction.Commit();
        _committed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        if (_disposed)
            return Task.CompletedTask;
        
        _transaction.Rollback();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (!_committed)
            {
                await RollbackAsync();
            }
            _transaction.Dispose();
            _disposed = true;
        }
    }
}
