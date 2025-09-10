using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

namespace Runner;

[TaskName("Publish.Web")]
[IsDependentOn(typeof(CompileTask))]
/// <summary>
/// Publishes the WebAPI.Minimal project to a folder under `artifacts/publish/WebAPI.Minimal/<Configuration>[/<Runtime>]`.
/// - Uses BuildContext.BuildConfiguration (from `--configuration`).
/// - Uses BuildContext.Runtime (from `--runtime`) to produce RID-specific output when provided.
/// - Uses BuildContext.PublishWebRoot for output path and BuildContext.RepoRoot for the project path.
/// Args pulled from BuildContext: BuildConfiguration, Runtime, PublishWebRoot, RepoRoot.
/// </summary>
public sealed class PublishWebTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var configuration = context.BuildConfiguration;
        var runtime = context.Runtime;

        var apiProject = context.RepoRoot.CombineWithFilePath(new FilePath("Source/Presentation/WebAPI.Minimal/WebAPI.Minimal.csproj"));

        var outDir = string.IsNullOrWhiteSpace(runtime)
            ? context.PublishWebRoot.Combine(new DirectoryPath(configuration))
            : context.PublishWebRoot.Combine(new DirectoryPath(configuration)).Combine(new DirectoryPath(runtime));

        context.Log.Information($"Publishing {apiProject.FullPath} -> {outDir.FullPath} ({configuration}{(string.IsNullOrWhiteSpace(runtime) ? string.Empty : ", runtime=" + runtime)})...");

        context.EnsureDirectoryExists(outDir);

        var publishSettings = new DotNetPublishSettings
        {
            Configuration = configuration,
            OutputDirectory = outDir,
            NoBuild = false,
        };

        if (!string.IsNullOrWhiteSpace(runtime))
        {
            publishSettings.Runtime = runtime;
            publishSettings.SelfContained = false;
        }

        context.DotNetPublish(apiProject.FullPath, publishSettings);

        context.Log.Information("Publish complete.");
    }
}

[TaskName("Publish")]
[IsDependentOn(typeof(PublishWebTask))]
[IsDependentOn(typeof(PackageWebTask))]
/// <summary>
/// Orchestrates publish and optional packaging.
/// - Always runs Publish.Web.
/// - Runs Package.Web only when BuildContext.PackageArtifacts is true (from `--package`).
/// Args pulled from BuildContext: PackageArtifacts.
/// </summary>
public sealed class PublishTask : FrostingTask<BuildContext>
{
}

[TaskName("Publish.All")]
[IsDependentOn(typeof(PublishTask))]
[IsDependentOn(typeof(PublishMigratorTask))]
[IsDependentOn(typeof(PackageMigratorTask))]
/// <summary>
/// Orchestrates publishing and optional packaging for both Web and Migrator.
/// - Runs Publish (Web) and Publish.Migrator always.
/// - Runs Package.Web and Package.Migrator only when --package=true.
/// </summary>
public sealed class PublishAllTask : FrostingTask<BuildContext>
{
}
