using System.Collections.Generic;
using System.Linq;

namespace SecureMediaRequestHandler.Constants
{
    /// <summary>
    /// Configuration for secure media access control
    /// </summary>
    public static class SecureMediaConfiguration
    {
        /// <summary>
        /// The base path for secure media in Sitecore CONTENT TREE (NOT media library)
        /// Files are stored as Sitecore items with File field
        /// </summary>
        public const string SecureMediaContentPath = "/sitecore/content/home/data/SecureMedia";

        /// <summary>
        /// The URL pattern for accessing secure media via HTTP handler
        /// Format: /api/securemedia/{state}/{filename}
        /// </summary>
        public const string SecureMediaUrlPattern = "/api/securemedia/";

        /// <summary>
        /// Mapping of state folder names to their required claim names
        /// Key: Folder name (case-insensitive)
        /// Value: Required claim name
        /// </summary>
        private static readonly Dictionary<string, string> StateFolderToClaim = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "alaska", "HasAlaskaState" },
            { "hawaii", "HasHawaiiState" },
            { "hi", "HasHawaiiState" },  // Alternative folder name for Hawaii
            { "restus", "HasRestUSState" },
            { "canada", "HasCanadaState" }
        };

        /// <summary>
        /// Gets the required claim name for a given state folder
        /// </summary>
        /// <param name="stateFolder">The state folder name (e.g., "alaska", "hawaii")</param>
        /// <returns>The claim name required to access this folder, or null if not found</returns>
        public static string GetRequiredClaim(string stateFolder)
        {
            if (string.IsNullOrWhiteSpace(stateFolder))
                return null;

            return StateFolderToClaim.TryGetValue(stateFolder.Trim(), out var claimName) 
                ? claimName 
                : null;
        }

                /// <summary>
                /// Gets all configured state folders
                /// </summary>
                public static IEnumerable<string> GetAllStateFolders()
                {
                    return StateFolderToClaim.Keys.ToList();
                }
            }
        }
