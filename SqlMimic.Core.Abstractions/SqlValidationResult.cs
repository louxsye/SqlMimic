using System;

namespace SqlMimic.Core.Abstractions
{
    /// <summary>
    /// Base SQL validation result
    /// </summary>
    public class SqlValidationResult
    {
        /// <summary>
        /// Whether the syntax is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error messages
        /// </summary>
#if NET462
        public string[] Errors { get; set; } = new string[0];
#else
        public string[] Errors { get; set; } = Array.Empty<string>();
#endif
    }
}