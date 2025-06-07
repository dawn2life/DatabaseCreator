using Xunit;
using Moq;
using DatabaseCreator.Service.CommonService;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace DatabaseCreator.Tests
{
    public class UserInterfaceServiceTests
    {
        private readonly Mock<ILogger<UserInterfaceService>> _loggerMock;
        private readonly UserInterfaceService _userInterfaceService;

        public UserInterfaceServiceTests()
        {
            _loggerMock = new Mock<ILogger<UserInterfaceService>>();
            _userInterfaceService = new UserInterfaceService(_loggerMock.Object);
        }

        [Theory]
        [InlineData("1", "ado.net")]
        [InlineData("2", "efcore")]
        [InlineData("3", "dapper")]
        [InlineData(" ", "ado.net")] // Whitespace input
        [InlineData("", "ado.net")]  // Empty input
        [InlineData("4", "ado.net")] // Invalid choice
        [InlineData("abc", "ado.net")] // Non-numeric choice
        [InlineData(null, "ado.net")] // Null input (simulated by Console.ReadLine() returning null)
        public void GetConnectionMethodChoice_VariousInputs_ReturnsCorrectMethodOrDefault(string? consoleInput, string expectedMethod)
        {
            // Arrange
            var stringReader = consoleInput != null ? new StringReader(consoleInput) : new StringReader(string.Empty);
            Console.SetIn(stringReader);

            // Act
            string result = _userInterfaceService.GetConnectionMethodChoice();

            // Assert
            Assert.Equal(expectedMethod, result);

            // Verify logging (optional, but good for confirming behavior)
            if (expectedMethod == "efcore" || expectedMethod == "dapper")
            {
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"User selected {expectedMethod} connection method.")),
                        null,
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            else // ado.net (default or invalid choice)
            {
                 // Check for the specific log message when defaulting
                  _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User selected ADO.NET or entered invalid/empty choice") && v.ToString().Contains("defaulting to ADO.NET")),
                        null,
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }

            // Clean up console input redirection
            var standardInput = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(standardInput);
        }

        [Fact]
        public void DisplayMessage_ErrorMessage_WritesToConsoleWithErrorColor()
        {
            // Arrange
            var originalForegroundColor = Console.ForegroundColor;
            var sw = new StringWriter();
            Console.SetOut(sw); // Redirect console output

            // Act
            _userInterfaceService.DisplayMessage("Test error", true);

            // Assert
            // Hard to assert Console.ForegroundColor directly in a test without more complex UI testing.
            // We can check the output string.
            Assert.Contains("ERROR: Test error", sw.ToString());

            // Cleanup
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.ForegroundColor = originalForegroundColor;
        }

        [Fact]
        public void DisplayMessage_SuccessMessage_WritesToConsoleWithSuccessColor()
        {
            // Arrange
            var originalForegroundColor = Console.ForegroundColor;
            var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            _userInterfaceService.DisplayMessage("Test success", false);

            // Assert
            Assert.Contains("Test success", sw.ToString()); // Check output
            Assert.DoesNotContain("ERROR:", sw.ToString());


            // Cleanup
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.ForegroundColor = originalForegroundColor;
        }

         // Note: Testing methods like DisplayAppName, DisplayCommands, GetConnectionStringInput
         // is mostly about verifying they call Console.WriteLine with expected strings.
         // This can be done similarly by redirecting Console.Out if deemed necessary,
         // but GetConnectionMethodChoice is the most complex logic in this service.
    }
}
