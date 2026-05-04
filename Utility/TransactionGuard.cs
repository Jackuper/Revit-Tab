using System;
using Autodesk.Revit.DB;

namespace Revit_Tab.Utility
{
    /// <summary>
    /// Helper class for safe transaction management with automatic rollback
    /// </summary>
    public class TransactionGuard : IDisposable
    {
        private readonly Transaction _transaction;
        private readonly string _commandName;
        private bool _committed;
        private bool _disposed;

        /// <summary>
        /// Creates and starts a new transaction with automatic error handling
        /// </summary>
        public TransactionGuard(Document doc, string transactionName, string commandName = null)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            _transaction = new Transaction(doc, transactionName);
            _commandName = commandName ?? transactionName;
            _committed = false;
            _disposed = false;

            try
            {
                _transaction.Start();
                ErrorHandler.LogInfo(_commandName, $"Transaction started: {transactionName}");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(_commandName, ex);
                throw;
            }
        }

        /// <summary>
        /// Commits the transaction
        /// </summary>
        public void Commit()
        {
            if (_committed)
                throw new InvalidOperationException("Transaction has already been committed.");

            if (_disposed)
                throw new ObjectDisposedException(nameof(TransactionGuard));

            try
            {
                _transaction.Commit();
                _committed = true;
                ErrorHandler.LogInfo(_commandName, $"Transaction committed: {_transaction.GetName()}");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(_commandName, ex);
                throw;
            }
        }

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        public void Rollback()
        {
            if (_committed)
                throw new InvalidOperationException("Cannot rollback a committed transaction.");

            if (_disposed)
                throw new ObjectDisposedException(nameof(TransactionGuard));

            try
            {
                if (_transaction.GetStatus() == TransactionStatus.Started)
                {
                    _transaction.RollBack();
                    ErrorHandler.LogInfo(_commandName, $"Transaction rolled back: {_transaction.GetName()}");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(_commandName, ex, showDialog: false);
                throw;
            }
        }

        /// <summary>
        /// Gets the underlying transaction (use with caution)
        /// </summary>
        public Transaction GetTransaction()
        {
            return _transaction;
        }

        /// <summary>
        /// Gets the transaction status
        /// </summary>
        public TransactionStatus GetStatus()
        {
            return _transaction.GetStatus();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // If transaction was started but not committed, roll it back
                if (!_committed && _transaction.GetStatus() == TransactionStatus.Started)
                {
                    _transaction.RollBack();
                    ErrorHandler.LogInfo(_commandName, $"Transaction auto-rolled back on dispose: {_transaction.GetName()}");
                }

                _transaction.Dispose();
            }
            catch (Exception ex)
            {
                // Log but don't throw from Dispose
                ErrorHandler.HandleError(_commandName, ex, showDialog: false);
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for safer transaction usage
    /// </summary>
    public static class TransactionExtensions
    {
        /// <summary>
        /// Executes an action within a safe transaction context
        /// </summary>
        public static bool ExecuteSafely(this Document doc, string transactionName, Action action, string commandName = null)
        {
            try
            {
                using (var guard = new TransactionGuard(doc, transactionName, commandName))
                {
                    action();
                    guard.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(commandName ?? transactionName, ex);
                return false;
            }
        }

        /// <summary>
        /// Executes a function within a safe transaction context and returns the result
        /// </summary>
        public static T ExecuteSafely<T>(this Document doc, string transactionName, Func<T> func, string commandName = null)
        {
            try
            {
                using (var guard = new TransactionGuard(doc, transactionName, commandName))
                {
                    T result = func();
                    guard.Commit();
                    return result;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(commandName ?? transactionName, ex);
                throw;
            }
        }
    }
}
