using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

namespace Runner;

[TaskName("Test")]
[IsDependentOn(typeof(TestUnitTask))]
[IsDependentOn(typeof(TestE2ETask))]
/// <summary>
/// Orchestrates all tests.
/// - Depends on unit and E2E tasks; each subtask self-skips based on BuildContext.RunUnitTests/RunE2ETests/RunAllTests (from `--unit`, `--e2e`, `--all`).
/// Args pulled from BuildContext: RunUnitTests, RunE2ETests, RunAllTests.
/// </summary>
public sealed class TestTask : FrostingTask<BuildContext>
{
}

[TaskName("Test.Unit")]
[IsDependentOn(typeof(CompileTask))]
/// <summary>
/// Runs unit tests only.
/// - Skips unless BuildContext.RunUnitTests or BuildContext.RunAllTests is true (from `--unit`, `--all`).
/// - Uses BuildContext.SolutionPath and BuildContext.BuildConfiguration; disables build (depends on Compile).
/// Args pulled from BuildContext: RunUnitTests, RunAllTests, BuildConfiguration.
/// </summary>
public sealed class TestUnitTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (!(context.RunAllTests || context.RunUnitTests))
        {
            context.Log.Warning("Unit tests disabled. Enable with --unit=true or --all=true.");
            return;
        }

        var solution = context.SolutionPath;
        var configuration = context.BuildConfiguration;
        var settings = new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
            Filter = "FullyQualifiedName!~E2E"
        };
        context.Log.Information("Running Unit tests only...");
        context.DotNetTest(solution.FullPath, settings);
    }
}

[TaskName("Test.E2E")]
[IsDependentOn(typeof(CompileTask))]
/// <summary>
/// Runs E2E tests only.
/// - Skips unless BuildContext.RunE2ETests or BuildContext.RunAllTests is true (from `--e2e`, `--all`).
/// - Uses BuildContext.BuildConfiguration and RepoRoot to locate the E2E test project; disables build (depends on Compile).
/// Args pulled from BuildContext: RunE2ETests, RunAllTests, BuildConfiguration, RepoRoot.
/// </summary>
public sealed class TestE2ETask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (!(context.RunAllTests || context.RunE2ETests))
        {
            context.Log.Warning("E2E tests disabled. Enable with --e2e=true or --all=true.");
            return;
        }

        var configuration = context.BuildConfiguration;
        var repoRoot = context.Environment.WorkingDirectory.Combine("../..");
        var e2eProject = repoRoot.CombineWithFilePath(new FilePath("Tests/Presentation/WebAPI.Minimal.E2E/WebAPI.Minimal.E2E.csproj"));
        var settings = new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
        };
        context.Log.Information("Running E2E tests only...");
        context.DotNetTest(e2eProject.FullPath, settings);
    }
}
