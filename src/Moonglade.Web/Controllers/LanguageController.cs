using Microsoft.AspNetCore.Localization;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController(ILogger<LanguageController> logger) : ControllerBase
{
    /// <summary>
    /// Sets the user's preferred language culture and redirects to the specified return URL.
    /// </summary>
    /// <param name="culture">The culture code (e.g., "en-US", "zh-CN")</param>
    /// <param name="returnUrl">The URL to redirect to after setting the language</param>
    /// <returns>A redirect result or error response</returns>
    [HttpPost("set")]
    public IActionResult SetLanguage([FromForm] string culture, [FromForm] string returnUrl)
    {
        try
        {
            // Validate culture parameter
            if (string.IsNullOrWhiteSpace(culture))
            {
                logger.LogWarning("SetLanguage called with null or empty culture parameter");
                return BadRequest("Culture parameter is required");
            }

            // Sanitize and validate culture format
            var sanitizedCulture = culture.Trim();
            if (!IsValidCultureFormat(sanitizedCulture))
            {
                logger.LogWarning("Invalid culture format provided: {Culture}", sanitizedCulture);
                return BadRequest("Invalid culture format");
            }

            // Validate return URL to prevent open redirect attacks
            var safeReturnUrl = ValidateReturnUrl(returnUrl);

            // Set the culture cookie
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false, // Allow JavaScript access for client-side localization
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            };

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(sanitizedCulture)),
                cookieOptions
            );

            logger.LogInformation("Language set to {Culture} for user, redirecting to {ReturnUrl}",
                sanitizedCulture, safeReturnUrl);

            return LocalRedirect(safeReturnUrl);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid culture argument: {Culture}", culture);
            return BadRequest("Invalid culture specified");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error setting language. Culture: {Culture}, ReturnUrl: {ReturnUrl}",
                culture, returnUrl);

            // Return to home page on any unexpected error to prevent potential security issues
            return LocalRedirect("~/");
        }
    }

    /// <summary>
    /// Validates that the culture string follows the expected format (e.g., "en-US", "zh-CN")
    /// </summary>
    /// <param name="culture">The culture string to validate</param>
    /// <returns>True if the culture format is valid, false otherwise</returns>
    private static bool IsValidCultureFormat(string culture)
    {
        try
        {
            var cultureInfo = new System.Globalization.CultureInfo(culture);
            return !string.IsNullOrEmpty(cultureInfo.Name);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates and sanitizes the return URL to prevent open redirect attacks
    /// </summary>
    /// <param name="returnUrl">The return URL to validate</param>
    /// <returns>A safe return URL</returns>
    private string ValidateReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "~/";
        }

        // Ensure the URL is local to prevent open redirect attacks
        if (Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        logger.LogWarning("Non-local return URL attempted: {ReturnUrl}", returnUrl);
        return "~/";
    }

}
