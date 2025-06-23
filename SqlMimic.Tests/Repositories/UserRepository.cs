using System.Data;
using System.Data.Common;
using SqlMimic.Tests.Models;

namespace SqlMimic.Tests.Repositories
{
    public class UserRepository
    {
        private readonly DbConnection _connection;

        public UserRepository(DbConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE Id = @id";
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@id";
            parameter.Value = id;
            command.Parameters.Add(parameter);

            await _connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3),
                    IsActive = reader.GetBoolean(4)
                };
            }

            return null;
        }

        public async Task<IEnumerable<User>> GetAllActiveAsync()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Email, CreatedAt, IsActive 
                FROM Users 
                WHERE IsActive = 1
                ORDER BY Name";

            await _connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3),
                    IsActive = reader.GetBoolean(4)
                });
            }

            return users;
        }

        public async Task<int> CreateAsync(User user)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (Name, Email, CreatedAt, IsActive) 
                VALUES (@name, @email, @createdAt, @isActive)";

            AddParameter(command, "@name", user.Name);
            AddParameter(command, "@email", user.Email);
            AddParameter(command, "@createdAt", user.CreatedAt);
            AddParameter(command, "@isActive", user.IsActive);

            await _connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            
            // In a real implementation, you would return the generated ID
            // For this mock, we'll return a fixed value
            return 1;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                UPDATE Users 
                SET Name = @name, Email = @email, IsActive = @isActive
                WHERE Id = @id";

            AddParameter(command, "@id", user.Id);
            AddParameter(command, "@name", user.Name);
            AddParameter(command, "@email", user.Email);
            AddParameter(command, "@isActive", user.IsActive);

            await _connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM Users WHERE Id = @id";
            
            AddParameter(command, "@id", id);

            await _connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> CountActiveUsersAsync()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE IsActive = 1";

            await _connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }
}