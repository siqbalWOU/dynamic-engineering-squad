/**This represents an error/exception type

used when a caller wants to stop the workflow

lets higher layers distinguish moderation rejection from other failures

That is also a separate concept from the service itself.**/

using System;

namespace InfrastructureApp.Services.ContentModeration;

public sealed class ContentModerationRejectedException : Exception
{
    // Optional category describing WHY the content was rejected.
    // Example categories from OpenAI moderation:
    // "hate", "violence", "sexual", "self-harm", etc.
    public string? Category { get; }

    // Constructor used when creating the exception.
    // Parameters:
    // message  -> human readable explanation of what failed
    // category -> optional moderation category that triggered the rejection
    public ContentModerationRejectedException(string message, string? category = null)
        : base(message)
    {
        // Stores the category so the caller can inspect it later.
        // Example: controller might display a specific warning message.
        Category = category;
    }
}