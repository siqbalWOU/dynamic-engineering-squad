//this file tests whether the duplicateImageException is working by detecting and rejecting duplicate images

using System;
using InfrastructureApp.Services.ImageHashing;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services.ImageHashing
{
    [TestFixture]
    public class DuplicateImageExceptionTests
    {
        [Test]
        public void Constructor_WithMessage_SetsMessageProperty()
        {
            // Arrange
            var expectedMessage = "Duplicate image detected.";

            // Act
            var ex = new DuplicateImageException(expectedMessage);

            // Assert
            Assert.That(ex.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void DuplicateImageException_IsDerivedFromException()
        {
            // Act
            var ex = new DuplicateImageException("Duplicate image detected.");

            // Assert
            Assert.That(ex, Is.InstanceOf<Exception>());
        }

        [Test]
        public void Constructor_CreatesExceptionInstance()
        {
            // Act
            var ex = new DuplicateImageException("Duplicate image detected.");

            // Assert
            Assert.That(ex, Is.Not.Null);
        }
    }
}