using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace SqlMimic.Core
{

    public sealed class MimicCommand : DbCommand
    {
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override UpdateRowSource UpdatedRowSource { get; set; }

        public override bool DesignTimeVisible { get; set; }

        private readonly DbParameterCollection _parameters = new MimicParameterCollection();
        protected override DbParameterCollection DbParameterCollection => _parameters;

        // test return value
        public object? MockReturnValue { get; set; }
        public bool MockHasRows { get; set; } = true;

        // Enhanced mock data setup
        internal MockDataSetup? MockDataSetup { get; set; }

        // Command execution tracking
        private static readonly List<MimicCommand> _executedCommands = new List<MimicCommand>();
        public static IReadOnlyList<MimicCommand> ExecutedCommands => _executedCommands;

        /// <summary>
        /// Clears the executed commands history
        /// </summary>
        public static void ClearExecutedCommands()
        {
            _executedCommands.Clear();
        }

        public override void Cancel() { }

        public override int ExecuteNonQuery()
        {
            RecordExecution();
            return (MockReturnValue as int?) ?? 1;
        }

        public override object? ExecuteScalar()
        {
            RecordExecution();

            if (MockDataSetup != null && MockDataSetup.Rows.Count > 0)
            {
                return MockDataSetup.Rows[0][0];
            }

            return MockReturnValue;
        }

        public override void Prepare() { }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            RecordExecution();

            if (MockDataSetup != null)
            {
                return new MimicDataReader(MockDataSetup.ColumnNames, MockDataSetup.Rows);
            }

            return new MimicDataReader(MockReturnValue, MockHasRows);
        }

        private void RecordExecution()
        {
            _executedCommands.Add(this);
            // Transaction already set in DbTransaction property
        }

        protected override DbParameter CreateDbParameter() => new MimicParameter();

        // Manage Connection, Transaction
        protected override DbConnection? DbConnection { get; set; }
        protected override DbTransaction? DbTransaction { get; set; }

        /// <summary>
        /// Transaction
        /// </summary>
        public MimicTransaction? UsedTransaction => DbTransaction as MimicTransaction;

        /// <summary>
        /// set DbConnection (internal)
        /// </summary>
        internal void SetConnection(DbConnection connection)
        {
            DbConnection = connection;
        }

        /// <summary>
        /// set DbTransaction (internal)
        /// </summary>
        internal void SetTransaction(DbTransaction transaction)
        {
            DbTransaction = transaction;
        }
    }
}