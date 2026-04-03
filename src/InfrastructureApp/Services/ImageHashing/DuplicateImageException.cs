//Do not create the report — the image is a duplicate.

using System;

namespace InfrastructureApp.Services.ImageHashing
{
    // We throw this when the uploaded image is either:
    // 1) an exact duplicate (same SHA-256), or
    // 2) too visually similar (pHash threshold)
    public sealed class DuplicateImageException : Exception
    {
        public DuplicateImageException(string message) : base(message)
        {
        }
    }
}