//moderation class
/**This is just a data contract

what was the result?

was moderation performed?
// Indicates whether moderation was actually performed.
    // Example: If the input text is empty or whitespace,
    // the service might skip moderation entirely.

was it allowed?
// Indicates whether the content passed moderation rules.
    // true  -> content is safe
    // false -> content violates moderation rules

was it flagged?
// Indicates whether the moderation system flagged the content.
    // Some moderation APIs distinguish between "allowed" and "flagged".

what was the reason?
// Optional reason explaining why the content was rejected.
    // Example values:
    // "hate"
    // "violence"
    // "sexual"
    // "self-harm"
    //
    // Nullable because there may not always be a reason provided.

It is not behavior-heavy. It is a value object / DTO-like result type.**/

// This file defines the result returned by the content moderation service.
// It does not contain behavior or logic — it simply represents data
// describing the outcome of a moderation check.


namespace InfrastructureApp.Services.ContentModeration;

public sealed record ContentModerationResult(bool Performed, bool IsAllowed, bool Flagged, string? Reason = null);