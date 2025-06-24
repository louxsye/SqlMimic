using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace SqlMimic.Core
{

    /// <summary>
    /// MimicDataReader that supports both single value and multiple rows/columns for testing
    /// </summary>
    public sealed class MimicDataReader : DbDataReader
    {
        // Legacy single-value mode
        private bool _hasRead = false;
        private readonly object? _returnValue;
        private readonly bool _hasRows;

        // Multi-row/column mode
        private readonly List<object[]>? _data;
        private int _currentRowIndex = -1;
        private readonly string[]? _columnNames;
        private readonly bool _isMultiRowMode;

        /// <summary>
        /// Creates a MimicDataReader for single value (legacy mode)
        /// </summary>
        public MimicDataReader(object? returnValue = null, bool hasRows = true)
        {
            _returnValue = returnValue;
            _hasRows = hasRows;
            _isMultiRowMode = false;
        }

        /// <summary>
        /// Creates a MimicDataReader for multiple rows and columns
        /// </summary>
        public MimicDataReader(string[] columnNames, List<object[]> data)
        {
#if NET462
            _columnNames = columnNames ?? new string[0];
#else
            _columnNames = columnNames ?? Array.Empty<string>();
#endif
            _data = data ?? new List<object[]>();
            _hasRows = _data.Count > 0;
            _isMultiRowMode = true;
        }

        public override bool Read()
        {
            if (_isMultiRowMode)
            {
                _currentRowIndex++;
                return _currentRowIndex < _data!.Count;
            }

            // Legacy mode
            if (!_hasRead && _hasRows)
            {
                _hasRead = true;
                return true;
            }
            return false;
        }

        public override object GetValue(int ordinal)
        {
            if (_isMultiRowMode)
            {
                if (_currentRowIndex < 0 || _currentRowIndex >= _data!.Count)
                {
                    throw new InvalidOperationException("No current row");
                }
                return _data[_currentRowIndex][ordinal];
            }

            // Legacy mode
            if (!_hasRows)
                throw new InvalidOperationException("No data present");

            if (_returnValue is object[] array && ordinal < array.Length)
            {
                return array[ordinal];
            }

            return _returnValue ?? 0.1m;
        }

        public override T GetFieldValue<T>(int ordinal)
        {
            var value = GetValue(ordinal);
            if (value is T)
                return (T)value;
            try
            {
                return (T)Convert.ChangeType(value, typeof(T))!;
            }
            catch
            {
                return default(T)!;
            }
        }

        public override int FieldCount => _isMultiRowMode ? _columnNames!.Length : 1;

        public override bool HasRows => _hasRows;

        public override bool IsClosed => false;

        public override int RecordsAffected => _isMultiRowMode ? -1 : 1;

        public override int Depth => 0;

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool GetBoolean(int ordinal)
        {
            return Convert.ToBoolean(GetValue(ordinal));
        }

        public override byte GetByte(int ordinal)
        {
            return Convert.ToByte(GetValue(ordinal));
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            var value = GetValue(ordinal);
            if (value is char c)
                return c;
            return Convert.ToChar(value);
        }

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            return GetFieldType(ordinal).Name;
        }

        public override DateTime GetDateTime(int ordinal)
        {
            var value = GetValue(ordinal);
            if (value is DateTime dt)
                return dt;
            return Convert.ToDateTime(value);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return Convert.ToDecimal(GetValue(ordinal));
        }

        public override double GetDouble(int ordinal)
        {
            return Convert.ToDouble(GetValue(ordinal));
        }

        public override Type GetFieldType(int ordinal)
        {
            if (_isMultiRowMode)
            {
                // For Dapper compatibility, try to determine type from first row if available
                if (_data!.Count > 0 && ordinal < _data[0].Length)
                {
                    var value = _data[0][ordinal];
                    return value?.GetType() ?? typeof(object);
                }
                return typeof(object);
            }

            // Legacy mode - try to get type from return value
            if (_returnValue is object[] array && ordinal < array.Length)
            {
                return array[ordinal]?.GetType() ?? typeof(object);
            }

            return _returnValue?.GetType() ?? typeof(object);
        }

        public override float GetFloat(int ordinal)
        {
            return Convert.ToSingle(GetValue(ordinal));
        }

        public override Guid GetGuid(int ordinal)
        {
            var value = GetValue(ordinal);
            if (value is Guid g)
                return g;
            return Guid.Parse(value.ToString() ?? string.Empty);
        }

        public override short GetInt16(int ordinal)
        {
            return Convert.ToInt16(GetValue(ordinal));
        }

        public override int GetInt32(int ordinal)
        {
            return Convert.ToInt32(GetValue(ordinal));
        }

        public override long GetInt64(int ordinal)
        {
            return Convert.ToInt64(GetValue(ordinal));
        }

        public override string GetName(int ordinal)
        {
            if (_isMultiRowMode)
            {
                return _columnNames![ordinal];
            }
            return "Value";
        }

        public override int GetOrdinal(string name)
        {
            if (_isMultiRowMode)
            {
                for (int i = 0; i < _columnNames!.Length; i++)
                {
                    if (string.Equals(_columnNames[i], name, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
                throw new IndexOutOfRangeException($"Column '{name}' not found");
            }

            return 0; // Legacy mode always returns ordinal 0
        }

        public override string GetString(int ordinal)
        {
            var value = GetValue(ordinal);
            return value?.ToString() ?? string.Empty;
        }

        public override int GetValues(object[] values)
        {
            if (_isMultiRowMode)
            {
                if (_currentRowIndex < 0 || _currentRowIndex >= _data!.Count)
                {
                    return 0;
                }

                var row = _data[_currentRowIndex];
                var count = Math.Min(values.Length, row.Length);
                Array.Copy(row, values, count);
                return count;
            }

            // Legacy mode
            if (values.Length > 0)
            {
                values[0] = GetValue(0);
                return 1;
            }
            return 0;
        }

        public override bool IsDBNull(int ordinal)
        {
            var value = GetValue(ordinal);
            return value == null || value == DBNull.Value;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override IEnumerator GetEnumerator()
        {
            if (_isMultiRowMode)
            {
                return _data!.GetEnumerator();
            }
            throw new NotImplementedException();
        }
    }
}