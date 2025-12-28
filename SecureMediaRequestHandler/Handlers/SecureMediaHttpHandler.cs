using System;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;
using SecureMediaRequestHandler.Constants;
using SecureMediaRequestHandler.Services;

namespace SecureMediaRequestHandler.Handlers
{
    /// <summary>
    /// Custom HTTP handler to serve secure media files from Sitecore content items
    /// URL Pattern: /api/securemedia/{state}/{filename}
    /// Files are stored in /sitecore/content/home/data/SecureMedia/{State}/ as Sitecore items
    /// </summary>
    public class SecureMediaHttpHandler : IHttpHandler
    {
        private readonly ClaimValidationService _claimValidationService;

        public SecureMediaHttpHandler()
        {
            _claimValidationService = new ClaimValidationService();
        }

        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                Log.Info("SecureMediaHandler: Processing request: " + context.Request.RawUrl, this);

                // Set no-cache headers immediately
                SetNoCacheHeaders(context);

                // Parse URL: /api/securemedia/{state}/{filename}
                var path = context.Request.Path;
                var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4 || !parts[0].Equals("api", StringComparison.OrdinalIgnoreCase) 
                    || !parts[1].Equals("securemedia", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warn($"SecureMediaHandler: Invalid path format: {path}", this);
                    context.Response.StatusCode = 404;
                    context.Response.Write("404 Not Found");
                    return;
                }

                var stateFolder = parts[2];
                var fileName = parts[3];

                Log.Info($"SecureMediaHandler: State='{stateFolder}', File='{fileName}'", this);

                // Get required claim for state
                var requiredClaim = SecureMediaConfiguration.GetRequiredClaim(stateFolder);
                if (string.IsNullOrWhiteSpace(requiredClaim))
                {
                    Log.Warn($"SecureMediaHandler: No claim mapping for state: '{stateFolder}'", this);
                    context.Response.StatusCode = 404;
                    context.Response.Write("404 Not Found");
                    return;
                }

                // Check authentication
                if (context.User == null || !context.User.Identity.IsAuthenticated)
                {
                    Log.Info($"SecureMediaHandler: User not authenticated", this);
                    context.Response.StatusCode = 401;
                    context.Response.StatusDescription = "Unauthorized";
                    context.Response.Write("401 Unauthorized: Authentication required");
                    return;
                }

                // Log user claims
                var claimsSummary = _claimValidationService.GetUserClaimsSummary(new HttpContextWrapper(context));
                Log.Info($"SecureMediaHandler: {claimsSummary}", this);

                // Validate user has required claim
                var hasAccess = _claimValidationService.ValidateUserClaim(requiredClaim, new HttpContextWrapper(context));
                if (!hasAccess)
                {
                    Log.Warn($"SecureMediaHandler: Access denied for user '{context.User.Identity.Name}' to state '{stateFolder}'", this);
                    context.Response.StatusCode = 403;
                    context.Response.StatusDescription = "Forbidden";
                    context.Response.Write($"403 Forbidden: You do not have permission to access {stateFolder} content");
                    return;
                }

                Log.Info($"SecureMediaHandler: Access granted for user '{context.User.Identity.Name}' to state '{stateFolder}'", this);

                // Get Sitecore item from content tree
                var itemPath = $"/sitecore/content/home/data/SecureMedia/{stateFolder}/{fileName}";
                var database = Sitecore.Context.Database ?? Sitecore.Data.Database.GetDatabase("web");
                var item = database.GetItem(itemPath);

                if (item == null)
                {
                    Log.Warn($"SecureMediaHandler: Item not found at path: {itemPath}", this);
                    context.Response.StatusCode = 404;
                    context.Response.Write("404 Not Found: File does not exist");
                    return;
                }

                // Get file field (assuming field name is "File" or "Media")
                var fileField = (Sitecore.Data.Fields.FileField)item.Fields["File"];
                if (fileField == null || fileField.MediaItem == null)
                {
                    Log.Warn($"SecureMediaHandler: No file field found on item: {itemPath}", this);
                    context.Response.StatusCode = 404;
                    context.Response.Write("404 Not Found: File field is empty");
                    return;
                }

                // Stream the file
                var mediaItem = fileField.MediaItem;
                var mediaStream = mediaItem.GetMediaStream();
                
                if (mediaStream == null)
                {
                    Log.Error($"SecureMediaHandler: Could not get media stream for: {itemPath}", this);
                    context.Response.StatusCode = 500;
                    context.Response.Write("500 Internal Server Error");
                    return;
                }

                // Set content type and headers
                context.Response.ContentType = mediaItem.MimeType;
                context.Response.AddHeader("Content-Disposition", $"inline; filename=\"{mediaItem.Name}{mediaItem.Extension}\"");
                context.Response.AddHeader("Content-Length", mediaStream.Length.ToString());

                Log.Info($"SecureMediaHandler: Serving file '{fileName}' to user '{context.User.Identity.Name}'", this);

                // Copy stream to response
                mediaStream.CopyTo(context.Response.OutputStream);
                context.Response.Flush();
            }
            catch (Exception ex)
            {
                Log.Error($"SecureMediaHandler: Unexpected error processing request: {context.Request.RawUrl}", ex, this);
                context.Response.StatusCode = 500;
                context.Response.Write("500 Internal Server Error");
            }
        }

        /// <summary>
        /// Sets HTTP headers to prevent caching of secure media
        /// </summary>
        private void SetNoCacheHeaders(HttpContext context)
        {
            try
            {
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                context.Response.Cache.SetNoStore();
                context.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                context.Response.Headers.Add("Pragma", "no-cache");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                Log.Debug("SecureMediaHandler: No-cache headers set", this);
            }
            catch (Exception ex)
            {
                Log.Warn("SecureMediaHandler: Error setting no-cache headers", ex, this);
            }
        }
    }
}
