using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Minimal.StartUp.Security;

public sealed class ApiKeyAuthMiddleware(RequestDelegate next, IOptions<PatAuthOptions> options, IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly PatAuthOptions _options = options.Value;
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldEnforce())
        {
            await _next(context);
            return;
        }

        // Accept either X-Api-Key or Authorization: Bearer <token>
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var auth = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = auth.Substring("Bearer ".Length).Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey) || !IsValid(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }

    private bool ShouldEnforce()
    {
        if (!_options.Enabled)
            return false;

        // Enforce only if a token is configured
        if (string.IsNullOrWhiteSpace(_options.Token))
            return false;

        // Skip in Development unless explicitly enabled
        if (_env.IsDevelopment() && !_options.EnforceInDevelopment)
            return false;

        return true;
    }

    private bool IsValid(string presentedToken)
    {
        var configured = _options.Token ?? string.Empty;
        return FixedTimeEquals(presentedToken, configured);
    }

    private static bool FixedTimeEquals(string tokenA, string tokenB)
    {
        // Compare in constant time to avoid timing attacks
        var ba = Encoding.UTF8.GetBytes(tokenA);
        var bb = Encoding.UTF8.GetBytes(tokenB);
        if (ba.Length != bb.Length) return false;
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
