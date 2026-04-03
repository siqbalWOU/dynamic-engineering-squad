/*These unit tests are verifying that your custom exception class behaves correctly. 
Since ContentModerationRejectedException is very small, the tests mainly confirm that the constructor stores data correctly 
and that the exception behaves like a normal .NET exception when thrown and caught. */

using NUnit.Framework;
using System;
using InfrastructureApp.Services.ContentModeration;

namespace InfrastructureApp_Tests.Services.ContentModeration
{
    [TestFixture]
    public class ContentModerationRejectedExceptionTests
    {
        //It verifies that the exception message is correctly passed to the base Exception class.
        [Test]
        public void Ctor_SetsMessage()
        {
            // Arrange
            var message = "Rejected by moderation";

            // Act
            var ex = new ContentModerationRejectedException(message);

            // Assert
            Assert.That(ex.Message, Is.EqualTo(message));
        }

        //It verifies that when a category is passed to the constructor, it is stored in the Category property.
        [Test]
        public void Ctor_WhenCategoryProvided_SetsCategory()
        {
            // Arrange
            var message = "Rejected by moderation";
            var category = "hate";

            // Act
            var ex = new ContentModerationRejectedException(message, category);

            // Assert
            Assert.That(ex.Category, Is.EqualTo(category));
        }

        //this test verifies that when category is not passed, it defaults to null.
        [Test]
        public void Ctor_WhenCategoryNotProvided_CategoryIsNull()
        {
            // Arrange
            var message = "Rejected by moderation";

            // Act
            var ex = new ContentModerationRejectedException(message);

            // Assert
            Assert.That(ex.Category, Is.Null);
        }

        //This verifies that when the exception is thrown and caught as a base Exception, the important data is still preserved.
        [Test]
        public void ThrowAndCatch_AsException_PreservesData()
        {
            // Arrange
            var message = "Rejected by moderation";
            var category = "violence";

            try
            {
                // Act
                throw new ContentModerationRejectedException(message, category);
            }
            catch (Exception e)
            {
                // Assert
                Assert.That(e, Is.TypeOf<ContentModerationRejectedException>());

                var typed = (ContentModerationRejectedException)e;
                Assert.That(typed.Message, Is.EqualTo(message));
                Assert.That(typed.Category, Is.EqualTo(category));
            }
        }

        //It verifies that the constructor does not set an InnerException, No unexpected inner exception.
        [Test]
        public void Ctor_DoesNotSetInnerException()
        {
            // Arrange
            var message = "Rejected by moderation";

            // Act
            var ex = new ContentModerationRejectedException(message, "self-harm");

            // Assert
            Assert.That(ex.InnerException, Is.Null);
        }
    }
}