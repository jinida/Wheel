using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WheelApp.Application.Common.Interfaces;

namespace WheelApp.Infrastructure.Identity;

/// <summary>
/// Service for accessing current authenticated user information from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string SystemUser = "system";

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the current user's ID from ClaimTypes.NameIdentifier.
    /// Returns null if no HttpContext, user not authenticated, or claim not found.
    /// </summary>
    public string? UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    /// <summary>
    /// Gets the current user's name from ClaimTypes.Name.
    /// Returns null if no HttpContext, user not authenticated, or claim not found.
    /// </summary>
    public string? UserName
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Name)?.Value;
        }
    }

    /// <summary>
    /// Indicates whether the current user is authenticated
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return false;

            return httpContext.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}
