using System;
using System.Linq;
using Xunit;
using SqlMimic.Core;

namespace SqlMimic.Tests
{
    public class MimicDataReaderTests
    {
        [Fact]
        public void MimicDataReader_LegacyMode_WorksAsExpected()
        {
            // Arrange
            var reader = new MimicDataReader("test value", true);

            // Act & Assert
            Assert.True(reader.HasRows);
            Assert.Equal(1, reader.FieldCount);
            Assert.True(reader.Read());
            Assert.Equal("test value", reader.GetValue(0));
            Assert.Equal("test value", reader.GetString(0));
            Assert.False(reader.Read()); // Should not read again
        }

        [Fact]
        public void MimicDataReader_LegacyMode_NoRows_WorksAsExpected()
        {
            // Arrange
            var reader = new MimicDataReader(null, false);

            // Act & Assert
            Assert.False(reader.HasRows);
            Assert.False(reader.Read());
        }

        [Fact]
        public void MimicDataReader_MultiRowMode_WorksAsExpected()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name", "IsActive" };
            var data = new List<object[]>
            {
                new object[] { 1, "John", true },
                new object[] { 2, "Jane", false }
            };
            var reader = new MimicDataReader(columnNames, data);

            // Act & Assert
            Assert.True(reader.HasRows);
            Assert.Equal(3, reader.FieldCount);
            
            // First row
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(0));
            Assert.Equal("John", reader.GetString(1));
            Assert.True(reader.GetBoolean(2));
            Assert.Equal("Id", reader.GetName(0));
            Assert.Equal("Name", reader.GetName(1));
            Assert.Equal("IsActive", reader.GetName(2));
            
            // Second row
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(0));
            Assert.Equal("Jane", reader.GetString(1));
            Assert.False(reader.GetBoolean(2));
            
            // No more rows
            Assert.False(reader.Read());
        }

        [Fact]
        public void MimicDataReader_MultiRowMode_EmptyData_WorksAsExpected()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name" };
            var data = new List<object[]>();
            var reader = new MimicDataReader(columnNames, data);

            // Act & Assert
            Assert.False(reader.HasRows);
            Assert.Equal(2, reader.FieldCount);
            Assert.False(reader.Read());
        }

        [Fact]
        public void MimicDataReader_GetOrdinal_WorksCorrectly()
        {
            // Arrange
            var columnNames = new[] { "UserId", "UserName", "Email" };
            var data = new List<object[]>
            {
                new object[] { 1, "test", "test@example.com" }
            };
            var reader = new MimicDataReader(columnNames, data);

            // Act & Assert
            Assert.Equal(0, reader.GetOrdinal("UserId"));
            Assert.Equal(1, reader.GetOrdinal("UserName"));
            Assert.Equal(2, reader.GetOrdinal("Email"));
            Assert.Equal(1, reader.GetOrdinal("username")); // Case insensitive
            
            // Should throw for non-existent column
            Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("NonExistent"));
        }
    }
}