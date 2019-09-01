// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           7.1.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Lexical.FileProvider.Package
{
    public class PackageFileProviderOptionsValidator : IValidateOptions<PackageFileProviderOptionsRecord>
    {
        private static readonly PackageFileProviderOptionsValidator level0 = new PackageFileProviderOptionsValidator(0);
        private static readonly PackageFileProviderOptionsValidator level1 = new PackageFileProviderOptionsValidator(1);
        private static readonly PackageFileProviderOptionsValidator level2 = new PackageFileProviderOptionsValidator(2);

        /// <summary>
        /// Validator that doesn't check if package loaders are found.
        /// </summary>
        public static IValidateOptions<PackageFileProviderOptionsRecord> Level0 => level0;

        /// <summary>
        /// Validator that tests if package loader types and assemblies are found.
        /// </summary>
        public static IValidateOptions<PackageFileProviderOptionsRecord> Level1 => level1;

        /// <summary>
        /// Validator that tries to instantiate package loaders.
        /// </summary>
        public static IValidateOptions<PackageFileProviderOptionsRecord> Level2 => level2;


        /// <summary>
        /// Level of validation:
        ///   0 - Checks package loader is not null
        ///   1 - Checks if type is loaded
        ///   2 - Tries to instantiate package loader
        /// </summary>
        public readonly int ValidationLevel;

        /// <summary>
        /// Create validator
        /// </summary>
        /// <param name="validationLevel">true if validator should try to load PackageLoader Type to verify that it exists</param>
        public PackageFileProviderOptionsValidator(int validationLevel)
        {
            this.ValidationLevel = validationLevel;
        }

        public ValidateOptionsResult Validate(string name, PackageFileProviderOptionsRecord options)
        {
            List<string> failMessages = null;

            // MaxMemorySnapshotLength
            if (options.MaxMemorySnapshotLength < 0L)
                (failMessages ?? (failMessages = new List<string>())).Add($"{nameof(PackageFileProviderOptionsRecord.MaxMemorySnapshotLength)} must be 0 or above.");

            // MaxTempSnapshotLength
            if (options.MaxTempSnapshotLength < 0L)
                (failMessages ?? (failMessages = new List<string>())).Add($"{nameof(PackageFileProviderOptionsRecord.MaxTempSnapshotLength)} must be 0 or above.");

            // MaxTempSnapshotLength
            if (options.CacheEvictTime < 0)
                (failMessages ?? (failMessages = new List<string>())).Add($"{nameof(PackageFileProviderOptionsRecord.CacheEvictTime)} must be 0 or above.");

            // Validate package loaders
            if (options.PackageLoaders == null)
            {
                (failMessages ?? (failMessages = new List<string>())).Add($"{nameof(PackageFileProviderOptionsRecord.PackageLoaders)} must not be null");
            }
            else
            {
                foreach (string packageLoader in options.PackageLoaders)
                {
                    if (string.IsNullOrEmpty(packageLoader))
                    {
                        (failMessages ?? (failMessages = new List<string>())).Add("Error in package loader value is null");
                        continue;
                    }

                    if (ValidationLevel>=1)
                    {
                        try
                        {
                            Type type = Type.GetType(packageLoader);

                            if (ValidationLevel>=2)
                            {
                                try
                                {
                                    Activator.CreateInstance(type);
                                } catch (Exception e)
                                {
                                    (failMessages ?? (failMessages = new List<string>())).Add($"Could not create \"{packageLoader}\", {e.GetType().FullName}: {e.Message}");
                                    continue;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            (failMessages ?? (failMessages = new List<string>())).Add($"Could not load type \"{packageLoader}\", {e.GetType().FullName}: {e.Message}");
                            continue;
                        }
                    }

                }
            }
            if (failMessages != null && failMessages.Count == 1) return ValidateOptionsResult.Fail(failMessages[0]);
            if (failMessages != null && failMessages.Count > 1) return ValidateOptionsResult.Fail(string.Join("\n", failMessages));
            return ValidateOptionsResult.Success;
        }
    }
}
