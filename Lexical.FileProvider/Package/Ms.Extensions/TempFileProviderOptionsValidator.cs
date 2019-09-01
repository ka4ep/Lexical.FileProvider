// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           7.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileProvider.Common;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexical.FileProvider.Package
{
    /// <summary>
    /// Validates <see cref="TempFileProviderOptions"/>.
    /// </summary>
    public class TempFileProviderOptionsValidator : IValidateOptions<TempFileProviderOptions>
    {
        private static readonly TempFileProviderOptionsValidator singleton = new TempFileProviderOptionsValidator();

        /// <summary>
        /// Validator that doesn't check if package loaders are found.
        /// </summary>
        public static IValidateOptions<TempFileProviderOptions> Singleton => singleton;

        /// <summary>
        /// Pattern that validates against valid filename characters
        /// </summary>
        public static readonly Regex InvalidFilenamePattern = new Regex($"[{Regex.Escape(new String(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled|RegexOptions.CultureInvariant);

        /// <summary>
        /// Pattern that validates against valid path characters (excluding %)
        /// </summary>
        public static readonly Regex InvalidPathPattern = new Regex($"[{Regex.Escape(new String(Path.GetInvalidPathChars().Where(c=>c!='%').ToArray()))}]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ValidateOptionsResult Validate(string name, TempFileProviderOptions options)
        {
            List<string> failMessages = null;

            // Options.Directory
            if (String.IsNullOrEmpty(options.Directory))
                (failMessages ?? (failMessages = new List<string>())).Add($"{nameof(TempFileProviderOptions)}.{nameof(TempFileProviderOptions.Directory)} must be configured.");
            else if (InvalidPathPattern.IsMatch(options.Directory))
                (failMessages ?? (failMessages = new List<string>())).Add($"Found invalid path characters in \"{options.Directory}\"");

            // Options.Prefix
            if (options.Prefix != null && InvalidFilenamePattern.IsMatch(options.Prefix))
                (failMessages ?? (failMessages = new List<string>())).Add($"Found invalid filename characters in \"{options.Prefix}\"");

            // Options.Suffix
            if (options.Suffix != null && InvalidFilenamePattern.IsMatch(options.Suffix))
                (failMessages ?? (failMessages = new List<string>())).Add($"Found invalid filename characters in \"{options.Suffix}\"");

            // Unwrap
            if (failMessages != null && failMessages.Count == 1) return ValidateOptionsResult.Fail(failMessages[0]);
            if (failMessages != null && failMessages.Count > 1) return ValidateOptionsResult.Fail(string.Join("\n", failMessages));
            return ValidateOptionsResult.Success;
        }
    }
}
