using Azure;
using Azure.Communication.Email;
using InfrastructureApp.Configuration;
using InfrastructureApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class AzureEmailServiceTests
    {
        private Mock<EmailClient> _mockEmailClient;
        private Mock<IOptions<EmailOptions>> _mockOptions;
        private Mock<ILogger<AzureEmailService>> _mockLogger;
        private AzureEmailService _service;
        private EmailOptions _options;

        [SetUp]
        public void Setup()
        {
            _mockEmailClient = new Mock<EmailClient>();
            _options = new EmailOptions { SenderEmail = "sender@example.com" };
            _mockOptions = new Mock<IOptions<EmailOptions>>();
            _mockOptions.Setup(x => x.Value).Returns(_options);
            _mockLogger = new Mock<ILogger<AzureEmailService>>();

            _service = new AzureEmailService(_mockOptions.Object, _mockLogger.Object, _mockEmailClient.Object);
        }

        [Test]
        public async Task SendEmailAsync_CallsEmailClientWithCorrectParameters()
        {
            // Arrange
            string recipient = "test@example.com";
            string subject = "Test Subject";
            string htmlMessage = "<h1>Test</h1>";

            _mockEmailClient.Setup(x => x.SendAsync(
                It.IsAny<WaitUntil>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<EmailSendOperation>());

            // Act
            await _service.SendEmailAsync(recipient, subject, htmlMessage);

            // Assert
            _mockEmailClient.Verify(x => x.SendAsync(
                WaitUntil.Completed,
                It.Is<EmailMessage>(m => 
                    m.SenderAddress == _options.SenderEmail &&
                    m.Recipients.To.Count == 1 &&
                    m.Recipients.To[0].Address == recipient &&
                    m.Content.Subject == subject &&
                    m.Content.Html == htmlMessage),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void SendEmailAsync_LogsErrorAndThrows_WhenClientFails()
        {
            // Arrange
            _mockEmailClient.Setup(x => x.SendAsync(
                It.IsAny<WaitUntil>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("API Error"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => 
                await _service.SendEmailAsync("test@example.com", "Subject", "Message"));

            _mockLogger.Verify(x => 
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()), 
                Times.Once);
        }
    }
}
