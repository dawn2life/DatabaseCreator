using Xunit;
using DatabaseCreator.Data.Infrastructure.Connection;
using System; // For NotSupportedException

namespace DatabaseCreator.Tests
{
    public class DatabaseConnectionFactoryTests
    {
        private readonly DatabaseConnectionFactory _factory;

        public DatabaseConnectionFactoryTests()
        {
            _factory = new DatabaseConnectionFactory();
        }

        [Theory]
        [InlineData("ado.net")]
        [InlineData("ADO.NET")]
        [InlineData("Ado.Net")]
        public void GetConnectionProvider_AdoNetRequested_ReturnsAdoNetConnectionProvider(string methodName)
        {
            var provider = _factory.GetConnectionProvider(methodName);
            Assert.IsType<AdoNetConnectionProvider>(provider);
        }

        [Theory]
        [InlineData("efcore")]
        [InlineData("EFCORE")]
        [InlineData("EfCore")]
        public void GetConnectionProvider_EfCoreRequested_ReturnsEfCoreConnectionProvider(string methodName)
        {
            var provider = _factory.GetConnectionProvider(methodName);
            Assert.IsType<EfCoreConnectionProvider>(provider);
        }

        [Theory]
        [InlineData("dapper")]
        [InlineData("DAPPER")]
        [InlineData("Dapper")]
        public void GetConnectionProvider_DapperRequested_ReturnsDapperConnectionProvider(string methodName)
        {
            var provider = _factory.GetConnectionProvider(methodName);
            Assert.IsType<DapperConnectionProvider>(provider);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("invalid_method")]
        [InlineData(null)]
        [InlineData("")]
        public void GetConnectionProvider_UnknownOrInvalidRequested_ThrowsNotSupportedException(string methodName)
        {
            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => _factory.GetConnectionProvider(methodName));
            Assert.Equal($"Connection method '{methodName}' is not supported.", exception.Message);
        }
    }
}
