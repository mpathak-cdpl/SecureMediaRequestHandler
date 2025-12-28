using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using Sitecore.Diagnostics;

namespace SecureMediaRequestHandler.Services
{
    /// <summary>
    /// Service for validating user claims for secure media access
    /// </summary>
    public class ClaimValidationService
    {
        /// <summary>
        /// Validates if the current user has the required claim to access a state-specific folder
        /// </summary>
        /// <param name="requiredClaim">The claim name required for access</param>
        /// <param name="httpContext">The current HTTP context</param>
        /// <returns>True if user has the claim with value "true", false otherwise</returns>
        public bool ValidateUserClaim(string requiredClaim, HttpContextBase httpContext)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requiredClaim))
                {
                    Log.Warn($"SecureMediaRequestHandler: Required claim is null or empty", this);
                    return false;
                }

                if (httpContext?.User == null)
                {
                    Log.Warn("SecureMediaRequestHandler: HttpContext or User is null", this);
                    return false;
                }

                // Check if user is authenticated
                if (!httpContext.User.Identity.IsAuthenticated)
                {
                    Log.Info($"SecureMediaRequestHandler: User is not authenticated for claim '{requiredClaim}'", this);
                    return false;
                }

                var claimsPrincipal = httpContext.User as ClaimsPrincipal;
                if (claimsPrincipal == null)
                {
                    Log.Warn($"SecureMediaRequestHandler: User is not a ClaimsPrincipal. User type: {httpContext.User.GetType().FullName}", this);
                    return false;
                }

                // Find the specific claim
                var claim = claimsPrincipal.Claims.FirstOrDefault(c => 
                    c.Type.Equals(requiredClaim, StringComparison.OrdinalIgnoreCase));

                if (claim == null)
                {
                    Log.Info($"SecureMediaRequestHandler: User '{httpContext.User.Identity.Name}' does not have claim '{requiredClaim}'", this);
                    return false;
                }

                // Check if claim value is "true" (case-insensitive)
                bool hasAccess = claim.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (hasAccess)
                {
                    Log.Info($"SecureMediaRequestHandler: User '{httpContext.User.Identity.Name}' has valid claim '{requiredClaim}' = '{claim.Value}'", this);
                }
                else
                {
                    Log.Info($"SecureMediaRequestHandler: User '{httpContext.User.Identity.Name}' has claim '{requiredClaim}' but value is '{claim.Value}' (expected 'true')", this);
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                Log.Error($"SecureMediaRequestHandler: Error validating claim '{requiredClaim}'", ex, this);
                return false;
            }
        }

        /// <summary>
        /// Gets a summary of all claims for the current user (for debugging/logging purposes)
        /// </summary>
        public string GetUserClaimsSummary(HttpContextBase httpContext)
        {
            try
            {
                if (httpContext?.User == null)
                    return "No user context available";

                if (!httpContext.User.Identity.IsAuthenticated)
                    return "User is not authenticated";

                var claimsPrincipal = httpContext.User as ClaimsPrincipal;
                if (claimsPrincipal == null)
                    return $"User is not ClaimsPrincipal (Type: {httpContext.User.GetType().FullName})";

                var claims = claimsPrincipal.Claims
                    .Select(c => $"{c.Type}={c.Value}")
                    .ToList();

                return $"User: {httpContext.User.Identity.Name}, Claims: [{string.Join(", ", claims)}]";
            }
            catch (Exception ex)
            {
                return $"Error getting claims summary: {ex.Message}";
            }
        }
    }
}
