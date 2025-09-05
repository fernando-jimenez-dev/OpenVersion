using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

namespace Runner;

[TaskName("Clean")]
/// <summary>
/// Cleans solution outputs and artifacts.
/// - Uses BuildContext.SolutionPath to run `dotnet clean` with BuildContext.BuildConfiguration (from `--configuration`).
/// - Empties BuildContext.ArtifactsDir (`artifacts/`).
/// Args pulled from BuildContext: BuildConfiguration.
/// </summary>
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var solution = context.SolutionPath;
        var configuration = context.BuildConfiguration;

        // Clean solution outputs
        context.Log.Information($"Cleaning solution {solution.FullPath} ({configuration})...");
        context.DotNetClean(solution.FullPath, new DotNetCleanSettings
        {
            Configuration = configuration
        });

        // Clean artifacts folder
        context.Log.Information($"Cleaning artifacts at {context.ArtifactsDir.FullPath}...");
        context.EnsureDirectoryExists(context.ArtifactsDir);
        context.CleanDirectory(context.ArtifactsDir);
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
/// <summary>
/// Restores NuGet packages for the solution.
/// - Uses BuildContext.SolutionPath.
/// Args pulled from BuildContext: none beyond solution path.
/// </summary>
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var solution = context.SolutionPath;
        context.Log.Information($"Restoring {solution.FullPath}...");
        context.DotNetRestore(solution.FullPath, new DotNetRestoreSettings());
    }
}

[TaskName("Compile")]
[IsDependentOn(typeof(RestoreTask))]
/// <summary>
/// Builds the solution.
/// - Uses BuildContext.SolutionPath and BuildContext.BuildConfiguration (from `--configuration`).
/// Args pulled from BuildContext: BuildConfiguration.
/// </summary>
public sealed class CompileTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var solution = context.SolutionPath;
        var configuration = context.BuildConfiguration;
        context.Log.Information($"Compiling {solution.FullPath} ({configuration})...");

        context.DotNetBuild(solution.FullPath, new DotNetBuildSettings
        {
            Configuration = configuration
        });

        context.Log.Information("Compile completed.");
    }
}
