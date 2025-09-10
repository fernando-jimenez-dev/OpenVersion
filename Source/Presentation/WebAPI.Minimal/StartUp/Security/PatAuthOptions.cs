namespace WebAPI.Minimal.StartUp.Security;

public sealed class PatAuthOptions
{
    // When true and a token is configured, middleware enforces PAT.
    public bool Enabled { get; set; } = true;

    // Optional: enforce even in Development. Defaults to false.
    public bool EnforceInDevelopment { get; set; } = false;

    // Single token (App Settings: PatAuth:Token or env PatAuth__Token)
    public string? Token { get; set; }
}
