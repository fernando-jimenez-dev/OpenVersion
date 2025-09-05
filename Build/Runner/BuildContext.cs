using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

public class BuildContext : FrostingContext
{
    public FilePath SolutionPath { get; }
    public string BuildConfiguration { get; }
    public Verbosity Verbosity { get; }
    public bool RunUnitTests { get; }
    public bool RunE2ETests { get; }
    public bool RunAllTests { get; }
    public DirectoryPath RepoRoot { get; }
    public DirectoryPath ArtifactsDir { get; }
    public DirectoryPath PublishRoot { get; }
    public DirectoryPath PublishWebRoot { get; }
    public DirectoryPath PackagesDir { get; }
    public bool PackageArtifacts { get; }
    public string Runtime { get; }
    public string SiteName { get; }
    public string AppPool { get; }
    public DirectoryPath SitePath { get; }
    public int SitePort { get; }
    public string DockerImage { get; }
    public string DockerTag { get; }
    public string DockerContainerName { get; }
    public int DockerHostPort { get; }
    public string DockerEnvironment { get; }
    public FilePath? DockerSecretsPath { get; }
    // Secrets materialization
    public FilePath SecretsTemplatePath { get; }
    public DirectoryPath SecretsOutDir { get; }
    public string SecretsFileName { get; }
    public FilePath? SecretsValuesPath { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        // Verbosity from Cake logging (already normalized by host)
        Verbosity = context.Log.Verbosity;

        // Global args
        var configuration = context.Arguments.GetArgument("configuration");
        BuildConfiguration = string.IsNullOrWhiteSpace(configuration) ? "Debug" : configuration;

        // Default to repository root (two levels up from Runner folder)
        RepoRoot = context.Environment.WorkingDirectory.Combine("../..");
        SolutionPath = RepoRoot.CombineWithFilePath(new FilePath("OpenVersion.sln"));

        // Standard output locations
        ArtifactsDir = RepoRoot.Combine("artifacts");
        PublishRoot = ArtifactsDir.Combine("publish");
        PublishWebRoot = PublishRoot.Combine("WebAPI.Minimal");
        PackagesDir = ArtifactsDir.Combine("packages");

        // Test flags
        // Defaults: Unit=true, E2E=false, All=false. If All=true, both unit and e2e run.
        bool ParseBoolOrDefault(string? value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            if (bool.TryParse(value, out var b)) return b;
            return defaultValue;
        }

        var unitArg = context.Arguments.GetArgument("unit");
        var e2eArg = context.Arguments.GetArgument("e2e");
        var allArg = context.Arguments.GetArgument("all");

        var runAll = ParseBoolOrDefault(allArg, false);
        var runUnit = ParseBoolOrDefault(unitArg, true);
        var runE2E = ParseBoolOrDefault(e2eArg, false);

        if (runAll)
        {
            runUnit = true;
            runE2E = true;
        }

        RunAllTests = runAll;
        RunUnitTests = runUnit;
        RunE2ETests = runE2E;

        // Packaging flag (controls Package.Web execution when orchestrating Publish)
        var packageArg = context.Arguments.GetArgument("package");
        PackageArtifacts = ParseBoolOrDefault(packageArg, false);

        // Common publish/deploy args
        var runtimeArg = context.Arguments.GetArgument("runtime");
        Runtime = string.IsNullOrWhiteSpace(runtimeArg) ? string.Empty : runtimeArg;

        var siteNameArg = context.Arguments.GetArgument("siteName");
        SiteName = string.IsNullOrWhiteSpace(siteNameArg) ? "OpenVersion" : siteNameArg;

        var appPoolArg = context.Arguments.GetArgument("appPool");
        AppPool = string.IsNullOrWhiteSpace(appPoolArg) ? "OpenVersion_AppPool" : appPoolArg;

        var sitePathArg = context.Arguments.GetArgument("sitePath");
        SitePath = string.IsNullOrWhiteSpace(sitePathArg) ? new DirectoryPath("C:/inetpub/wwwroot/OpenVersion") : new DirectoryPath(sitePathArg);

        var portArg = context.Arguments.GetArgument("port");
        if (!string.IsNullOrWhiteSpace(portArg) && int.TryParse(portArg, out var parsedPort))
        {
            SitePort = parsedPort;
        }
        else
        {
            SitePort = 8080;
        }

        // Docker args
        var dockerImageArg = context.Arguments.GetArgument("dockerImage");
        DockerImage = string.IsNullOrWhiteSpace(dockerImageArg) ? "openversion-web" : dockerImageArg;

        var dockerTagArg = context.Arguments.GetArgument("dockerTag");
        DockerTag = string.IsNullOrWhiteSpace(dockerTagArg) ? "local" : dockerTagArg;

        var dockerNameArg = context.Arguments.GetArgument("dockerName");
        DockerContainerName = string.IsNullOrWhiteSpace(dockerNameArg) ? "openversion-web" : dockerNameArg;

        var dockerPortArg = context.Arguments.GetArgument("dockerPort");
        if (!string.IsNullOrWhiteSpace(dockerPortArg) && int.TryParse(dockerPortArg, out var parsedDockerPort))
        {
            DockerHostPort = parsedDockerPort;
        }
        else
        {
            DockerHostPort = 8080;
        }

        var dockerEnvArg = context.Arguments.GetArgument("dockerEnv");
        DockerEnvironment = string.IsNullOrWhiteSpace(dockerEnvArg) ? "Development" : dockerEnvArg;

        var dockerSecretsArg = context.Arguments.GetArgument("dockerSecretsPath");
        if (string.IsNullOrWhiteSpace(dockerSecretsArg))
        {
            DockerSecretsPath = null;
        }
        else
        {
            DockerSecretsPath = System.IO.Path.IsPathRooted(dockerSecretsArg)
                ? new FilePath(dockerSecretsArg)
                : RepoRoot.CombineWithFilePath(new FilePath(dockerSecretsArg));
        }

        // Secrets materialization defaults
        var secretsTemplateArg = context.Arguments.GetArgument("secretsTemplatePath");
        if (string.IsNullOrWhiteSpace(secretsTemplateArg))
        {
            SecretsTemplatePath = RepoRoot.CombineWithFilePath(new FilePath("Source/Presentation/WebAPI.Minimal/secrets.template.json"));
        }
        else
        {
            SecretsTemplatePath = System.IO.Path.IsPathRooted(secretsTemplateArg)
                ? new FilePath(secretsTemplateArg)
                : RepoRoot.CombineWithFilePath(new FilePath(secretsTemplateArg));
        }

        var secretsOutDirArg = context.Arguments.GetArgument("secretsOutDir");
        if (string.IsNullOrWhiteSpace(secretsOutDirArg))
        {
            SecretsOutDir = SitePath;
        }
        else
        {
            SecretsOutDir = System.IO.Path.IsPathRooted(secretsOutDirArg)
                ? new DirectoryPath(secretsOutDirArg)
                : RepoRoot.Combine(new DirectoryPath(secretsOutDirArg));
        }

        var secretsFileNameArg = context.Arguments.GetArgument("secretsFileName");
        SecretsFileName = string.IsNullOrWhiteSpace(secretsFileNameArg) ? "secrets.json" : secretsFileNameArg;

        var secretsValuesPathArg = context.Arguments.GetArgument("secretsValuesPath");
        if (string.IsNullOrWhiteSpace(secretsValuesPathArg))
        {
            SecretsValuesPath = null;
        }
        else
        {
            SecretsValuesPath = System.IO.Path.IsPathRooted(secretsValuesPathArg)
                ? new FilePath(secretsValuesPathArg)
                : RepoRoot.CombineWithFilePath(new FilePath(secretsValuesPathArg));
        }
    }
}
