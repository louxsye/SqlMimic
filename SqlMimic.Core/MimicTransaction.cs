using System;
using System.Data;
using System.Data.Common;

namespace SqlMimic.Core
{

    public sealed class MimicTransaction : DbTransaction
    {
        private readonly MimicConnection _connection;
        private bool _isCommitted = false;
        private bool _isRolledBack = false;
        private bool _isDisposed = false;

        public MimicTransaction(MimicConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _connection = connection;
            IsolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel { get; }

        protected override DbConnection DbConnection => _connection;

        /// <summary>
        /// Whether the transaction has been committed
        /// </summary>
        public bool IsCommitted => _isCommitted;

        /// <summary>
        /// Whether the transaction has been rolled back
        /// </summary>
        public bool IsRolledBack => _isRolledBack;

        /// <summary>
        /// Whether the transaction has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;

        public override void Commit()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Transaction has been disposed");
            if (_isCommitted)
                throw new InvalidOperationException("Transaction has already been committed");
            if (_isRolledBack)
                throw new InvalidOperationException("Transaction has been rolled back");

            _isCommitted = true;
            _connection.OnTransactionCompleted(this);
        }

        public override void Rollback()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Transaction has been disposed");
            if (_isCommitted)
                throw new InvalidOperationException("Transaction has already been committed");
            if (_isRolledBack)
                throw new InvalidOperationException("Transaction has already been rolled back");

            _isRolledBack = true;
            _connection.OnTransactionCompleted(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                // Automatically rollback if neither commit nor rollback has been performed
                if (!_isCommitted && !_isRolledBack)
                {
                    try
                    {
                        Rollback();
                    }
                    catch
                    {
                        // Ignore exceptions during Dispose
                    }
                }
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
