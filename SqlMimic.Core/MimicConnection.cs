using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace SqlMimic.Core
{

    public sealed class MimicConnection : DbConnection
    {
        private readonly List<MimicCommand> _cmds = new List<MimicCommand>();
        public IReadOnlyList<MimicCommand> Commands => _cmds;

        // test return value
        public object MockReturnValue { get; set; }
        public bool? MockHasRows { get; set; }
        // multiple return value
        private readonly Queue<object> _mockReturnValues = new Queue<object>();
        private readonly Queue<bool> _mockHasRowsValues = new Queue<bool>();

        // Enhanced mock data setup
        private readonly Queue<MockDataSetup> _mockDataSetups = new Queue<MockDataSetup>();
        private MockDataSetup? _defaultMockData;

        /// <summary>
        /// Sets multiple return values in sequence
        /// </summary>
        public void SetupSequentialReturnValues(params object[] values)
        {
            _mockReturnValues.Clear();
            foreach (var value in values)
            {
                _mockReturnValues.Enqueue(value);
            }
        }

        /// <summary>
        /// Sets multiple HasRows values in sequence
        /// </summary>
        public void SetupSequentialHasRowsValues(params bool[] values)
        {
            _mockHasRowsValues.Clear();
            foreach (var value in values)
            {
                _mockHasRowsValues.Enqueue(value);
            }
        }

        /// <summary>
        /// Sets up mock data with column names and rows for next command
        /// </summary>
        public void SetupMockData(string[] columnNames, params object[][] rows)
        {
            var setup = new MockDataSetup(columnNames, rows);
            _mockDataSetups.Enqueue(setup);
        }

        /// <summary>
        /// Sets up default mock data that will be used for all commands
        /// </summary>
        public void SetupDefaultMockData(string[] columnNames, params object[][] rows)
        {
            _defaultMockData = new MockDataSetup(columnNames, rows);
        }

        /// <summary>
        /// Clears all mock data setups
        /// </summary>
        public void ClearMockData()
        {
            _mockDataSetups.Clear();
            _defaultMockData = null;
        }

        protected override DbCommand CreateDbCommand()
        {
            var returnValue = _mockReturnValues.Count > 0 ? _mockReturnValues.Dequeue() : MockReturnValue;
            var hasRows = _mockHasRowsValues.Count > 0 ? _mockHasRowsValues.Dequeue() : (MockHasRows ?? true);

            // Check for mock data setup
            MockDataSetup? mockData = null;
            if (_mockDataSetups.Count > 0)
            {
                mockData = _mockDataSetups.Dequeue();
            }
            else if (_defaultMockData != null)
            {
                mockData = _defaultMockData;
            }

            var cmd = new MimicCommand
            {
                MockReturnValue = returnValue,
                MockHasRows = hasRows,
                MockDataSetup = mockData
            };

            // setting DbConnection
            cmd.SetConnection(this);

            // Set if current transaction exists
            if (_currentTransaction != null && !_currentTransaction.IsCommitted && !_currentTransaction.IsRolledBack)
            {
                cmd.SetTransaction(_currentTransaction);
            }

            _cmds.Add(cmd);
            return cmd;
        }
        // State always Open
        public override ConnectionState State => ConnectionState.Open;

        public override string ConnectionString { get => "Data Source=TestDb"; set { } }

        public override string Database => "TestDb";

        public override string DataSource => "TestDb";

        public override string ServerVersion => "1.0";

        public override void Open() { }
        public override void Close() { }

        // Transaction Management
        private readonly List<MimicTransaction> _transactions = new List<MimicTransaction>();
        public IReadOnlyList<MimicTransaction> Transactions => _transactions;
        private MimicTransaction? _currentTransaction;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            var transaction = new MimicTransaction(this, isolationLevel);
            _transactions.Add(transaction);
            _currentTransaction = transaction;
            return transaction;
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Notification on transaction completion
        /// </summary>
        internal void OnTransactionCompleted(MimicTransaction transaction)
        {
            if (_currentTransaction == transaction)
            {
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Mock data setup for MimicConnection
    /// </summary>
    internal class MockDataSetup
    {
        public string[] ColumnNames { get; }
        public List<object[]> Rows { get; }

        public MockDataSetup(string[] columnNames, object[][] rows)
        {
            ColumnNames = columnNames ?? new string[0];
            Rows = new List<object[]>(rows ?? new object[0][]);
        }
    }
}