using Xunit;
using Moq;
using DatabaseCreator.Domain.Services;
using DatabaseCreator.Domain.Repositories;
using DatabaseCreator.Service.CommonService;
using System.Collections.Generic;
using DatabaseCreator.Domain.Dto;
using System.Linq;
using AutoMapper;
using DatabaseCreator.Domain.Models;
using DatabaseCreator.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DatabaseCreator.Tests
{
    public class DatabaseOperationServiceTests
    {
        private readonly Mock<IDatabaseOperationRepository> _databaseOperationRepositoryMock;
        private readonly Mock<IUserInterfaceService> _userInterfaceServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly DatabaseOperationService _databaseOperationService;
        private readonly Mock<ILogger<DatabaseOperationService>> _loggerMock;

        public DatabaseOperationServiceTests()
        {
            _databaseOperationRepositoryMock = new Mock<IDatabaseOperationRepository>();
            _userInterfaceServiceMock = new Mock<IUserInterfaceService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<DatabaseOperationService>>();
            _databaseOperationService = new DatabaseOperationService(
                _databaseOperationRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        private string CreateTempSqlFile(string content, string? specificFileName = null)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "TestScripts");
            Directory.CreateDirectory(tempDir);
            var fileName = string.IsNullOrWhiteSpace(specificFileName) ? Path.GetRandomFileName() + ".sql" : specificFileName;
            var tempFilePath = Path.Combine(tempDir, fileName);
            File.WriteAllText(tempFilePath, content);
            return tempFilePath;
        }

        private void DeleteTempFile(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void SingleExecution_ValidNames_CreatesDatabasesSuccessfully()
        {
            var databaseNames = new List<string> { "db1", "db2" };
            var expectedCreatedDbs = databaseNames.Select(name => new DbInfo { DbName = name, IsCreated = true }).ToList();

            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution(It.IsAny<string>()));
            _databaseOperationRepositoryMock.Setup(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()));
            _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                       .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

            var result = _databaseOperationService.SingleExecution(databaseNames, null);

            Assert.NotNull(result);
            Assert.Equal(expectedCreatedDbs.Count, result.Count);
            for (int i = 0; i < expectedCreatedDbs.Count; i++)
            {
                Assert.Equal(expectedCreatedDbs[i].DbName, result[i]);
            }

            foreach (var name in databaseNames)
            {
                _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(name), Times.Once);
            }
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Count == databaseNames.Count)), Times.Once);
        }

        [Fact]
        public void SingleExecution_SomeFailures_ReturnsOnlySuccessfullyCreatedDbs()
        {
            var databaseNames = new List<string> { "db1", "fail_db", "db2" };
            var successfullyCreatedNames = new List<string> { "db1", "db2" };

            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution("db1"));
            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution("fail_db"))
                                           .Throws(new DatabaseOperationException("CreateDbWithSingleExecution", "fail_db", "Mocked failure for fail_db", new Exception()));
            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution("db2"));
            _databaseOperationRepositoryMock.Setup(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()));
            _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                       .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

            var result = _databaseOperationService.SingleExecution(databaseNames, null);

            Assert.NotNull(result);
            Assert.Equal(successfullyCreatedNames.Count, result.Count);
            for (int i = 0; i < successfullyCreatedNames.Count; i++)
            {
                Assert.Equal(successfullyCreatedNames[i], result[i]);
            }

            foreach (var name in databaseNames)
            {
                _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(name), Times.Once);
            }
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list =>
                list.Count == databaseNames.Count &&
                list.First(db => db.DbName == "db1").IsCreated == true &&
                list.First(db => db.DbName == "fail_db").IsCreated == false &&
                list.First(db => db.DbName == "db2").IsCreated == true
            )), Times.Once);
        }

        [Fact]
        public void SingleExecution_NullInput_ReturnsNull()
        {
            List<string>? databaseNames = null;
            var result = _databaseOperationService.SingleExecution(databaseNames, null);
            Assert.Null(result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(It.IsAny<string>()), Times.Never);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()), Times.Never);
        }

        [Fact]
        public void SingleExecution_EmptyInput_ReturnsEmptyList()
        {
            var databaseNames = new List<string>();
            var result = _databaseOperationService.SingleExecution(databaseNames, null);
            Assert.NotNull(result);
            Assert.Empty(result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(It.IsAny<string>()), Times.Never);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()), Times.Never);
        }

        [Fact]
        public void Batch_ValidNames_CreatesAllDatabasesSuccessfully()
        {
            var databaseNames = new List<string> { "batchDb1", "batchDb2" };
            var expectedDbInfos = databaseNames.Select(name => new DbInfo { DbName = name, IsCreated = true }).ToList();

            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithBatch(databaseNames));
            _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                       .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());
            _databaseOperationRepositoryMock.Setup(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()));

            var result = _databaseOperationService.Batch(databaseNames, null);

            Assert.NotNull(result);
            Assert.Equal(databaseNames.Count, result.Count);
            Assert.Equal(databaseNames, result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(databaseNames), Times.Once);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list =>
                list.Count == expectedDbInfos.Count &&
                list.All(actual => expectedDbInfos.Any(expected => actual.DbName == expected.DbName && actual.IsCreated == expected.IsCreated))
            )), Times.Once);
        }

        [Fact]
        public void Batch_RepositoryThrowsDbOperationException_ReturnsEmptyListAndLogsHistoryAsFailure()
        {
            var databaseNames = new List<string> { "batchDb3", "batchDb4" };
            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithBatch(databaseNames))
                                           .Throws(new DatabaseOperationException("CreateDbWithBatch", "Mocked batch failure", new Exception()));

            _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                       .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());
            _databaseOperationRepositoryMock.Setup(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()));

            var result = _databaseOperationService.Batch(databaseNames, null);

            Assert.NotNull(result);
            Assert.Empty(result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(databaseNames), Times.Once);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list =>
                list.Count == databaseNames.Count &&
                list.All(dbInfo => !dbInfo.IsCreated)
            )), Times.Once);
        }

        [Fact]
        public void Batch_NullInput_ReturnsNull()
        {
            List<string>? databaseNames = null;
            var result = _databaseOperationService.Batch(databaseNames, null);
            Assert.Null(result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(It.IsAny<List<string>>()), Times.Never);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()), Times.Never);
        }

        [Fact]
        public void Batch_EmptyInput_ReturnsEmptyList()
        {
            var databaseNames = new List<string>();
            var result = _databaseOperationService.Batch(databaseNames, null);
            Assert.NotNull(result);
            Assert.Empty(result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(It.IsAny<List<string>>()), Times.Never);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.IsAny<List<DbInfo>>()), Times.Never);
        }

        // --- Tests for SingleExecution with SQL Script ---

        [Fact]
        public void SingleExecution_WithValidScript_ExecutesScriptSuccessfully()
        {
            var dbName = "testDbScript";
            var scriptContent = "CREATE TABLE TestTable (ID INT);";
            string tempScriptPath = CreateTempSqlFile(scriptContent);

            try
            {
                _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution(dbName));
                _databaseOperationRepositoryMock.Setup(repo => repo.ExecuteSqlScript(dbName, scriptContent));
                _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                           .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

                var result = _databaseOperationService.SingleExecution(new List<string> { dbName }, tempScriptPath);

                Assert.NotNull(result);
                Assert.Contains(dbName, result);
                _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(dbName), Times.Once);
                _databaseOperationRepositoryMock.Verify(repo => repo.ExecuteSqlScript(dbName, scriptContent), Times.Once);
                _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Any(di => di.DbName == dbName && di.IsCreated))), Times.Once);
            }
            finally
            {
                DeleteTempFile(tempScriptPath);
            }
        }

        [Fact]
        public void SingleExecution_WithNonExistentScriptFile_LogsErrorAndCreatesDb()
        {
            var dbName = "testDbNoScriptFile";
            var nonExistentScriptPath = "non_existent_script.sql";

            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution(dbName));
            _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                       .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

            var result = _databaseOperationService.SingleExecution(new List<string> { dbName }, nonExistentScriptPath);

            Assert.NotNull(result);
            Assert.Contains(dbName, result);
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(dbName), Times.Once);
            _databaseOperationRepositoryMock.Verify(repo => repo.ExecuteSqlScript(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error reading SQL script file")),
                    It.IsAny<System.IO.FileNotFoundException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
             _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Any(di => di.DbName == dbName && di.IsCreated))), Times.Once);
        }

        [Fact]
        public void SingleExecution_ScriptExecutionFailsInRepository_LogsErrorAndCreatesDb()
        {
            var dbName = "testDbScriptFail";
            var scriptContent = "INVALID SQL;";
            string tempScriptPath = CreateTempSqlFile(scriptContent);

            try
            {
                _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithSingleExecution(dbName));
                _databaseOperationRepositoryMock.Setup(repo => repo.ExecuteSqlScript(dbName, scriptContent))
                                               .Throws(new DatabaseOperationException("ExecuteSqlScript", dbName, "Mocked script execution failure", new Exception()));
                _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                           .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

                var result = _databaseOperationService.SingleExecution(new List<string> { dbName }, tempScriptPath);

                Assert.NotNull(result);
                Assert.Contains(dbName, result);
                _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithSingleExecution(dbName), Times.Once);
                _databaseOperationRepositoryMock.Verify(repo => repo.ExecuteSqlScript(dbName, scriptContent), Times.Once);

                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("DatabaseOperationException for database testDbScriptFail")),
                        It.IsAny<DatabaseOperationException>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
                _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Any(di => di.DbName == dbName && di.IsCreated))), Times.Once);
            }
            finally
            {
                DeleteTempFile(tempScriptPath);
            }
        }

        // --- Tests for Batch with SQL Script ---

        [Fact]
        public void Batch_WithValidScript_ExecutesScriptSuccessfullyForAllDbs()
        {
            var dbNames = new List<string> { "batchDbScript1", "batchDbScript2" };
            var scriptContent = "CREATE TABLE BatchTest (ID INT);";
            string tempScriptPath = CreateTempSqlFile(scriptContent);

            try
            {
                _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithBatch(dbNames));
                _databaseOperationRepositoryMock.Setup(repo => repo.ExecuteSqlScript(It.IsAny<string>(), scriptContent));
                _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                           .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

                var result = _databaseOperationService.Batch(dbNames, tempScriptPath);

                Assert.NotNull(result);
                Assert.Equal(dbNames.Count, result.Count);
                dbNames.ForEach(dbName => Assert.Contains(dbName, result));
                _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(dbNames), Times.Once);
                dbNames.ForEach(dbName =>
                    _databaseOperationRepositoryMock.Verify(repo => repo.ExecuteSqlScript(dbName, scriptContent), Times.Once)
                );
                _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Count == dbNames.Count && list.All(di => di.IsCreated))), Times.Once);
            }
            finally
            {
                DeleteTempFile(tempScriptPath);
            }
        }

        [Fact]
        public void Batch_WithNonExistentScriptFile_LogsErrorAndCreatesDbs()
        {
            var dbNames = new List<string> { "batchDbNoScript1", "batchDbNoScript2" };
            var nonExistentScriptPath = "non_existent_batch_script.sql";

            _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithBatch(dbNames));
            _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                        .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

            var result = _databaseOperationService.Batch(dbNames, nonExistentScriptPath);

            Assert.NotNull(result);
            Assert.Equal(dbNames.Count, result.Count);
            dbNames.ForEach(dbName => Assert.Contains(dbName, result));
            _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(dbNames), Times.Once);
            _databaseOperationRepositoryMock.Verify(repo => repo.ExecuteSqlScript(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error reading SQL script file")),
                    It.IsAny<System.IO.FileNotFoundException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Count == dbNames.Count && list.All(di => di.IsCreated))), Times.Once);
        }

        [Fact]
        public void Batch_ScriptExecutionFailsForOneDb_LogsErrorAndAttemptsForAllDbs()
        {
            var dbNames = new List<string> { "batchScriptOk1", "batchScriptFail", "batchScriptOk2" };
            var scriptContent = "CREATE TABLE BatchTest (ID INT);";
            string tempScriptPath = CreateTempSqlFile(scriptContent);

            try
            {
                _databaseOperationRepositoryMock.Setup(repo => repo.CreateDbWithBatch(dbNames));
                _databaseOperationRepositoryMock.Setup(repo => repo.ExecuteSqlScript("batchScriptOk1", scriptContent));
                _databaseOperationRepositoryMock.Setup(repo => repo.ExecuteSqlScript("batchScriptFail", scriptContent))
                                               .Throws(new DatabaseOperationException("ExecuteSqlScript", "batchScriptFail", "Mocked script failure for batchScriptFail", new Exception()));
                _databaseOperationRepositoryMock.Setup(repo => repo.ExecuteSqlScript("batchScriptOk2", scriptContent));
                _mapperMock.Setup(m => m.Map<List<DbInfo>>(It.IsAny<List<DbInfodto>>()))
                           .Returns((List<DbInfodto> dtos) => dtos.Select(dto => new DbInfo { DbName = dto.DbName, IsCreated = dto.IsCreated }).ToList());

                var result = _databaseOperationService.Batch(dbNames, tempScriptPath);

                Assert.NotNull(result);
                Assert.Equal(dbNames.Count, result.Count);
                dbNames.ForEach(dbName => Assert.Contains(dbName, result));

                _databaseOperationRepositoryMock.Verify(repo => repo.CreateDbWithBatch(dbNames), Times.Once);
                dbNames.ForEach(dbName =>
                    _databaseOperationRepositoryMock.Verify(repo => repo.ExecuteSqlScript(dbName, scriptContent), Times.Once)
                );

                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error executing SQL script on batch-created database batchScriptFail")),
                        It.IsAny<DatabaseOperationException>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
                _databaseOperationRepositoryMock.Verify(repo => repo.AddCreatedDb(It.Is<List<DbInfo>>(list => list.Count == dbNames.Count && list.All(di => di.IsCreated))), Times.Once);
            }
            finally
            {
                DeleteTempFile(tempScriptPath);
            }
        }
    }
}
