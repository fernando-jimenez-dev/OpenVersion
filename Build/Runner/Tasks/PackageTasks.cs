using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Common.IO;
using System.IO.Compression;

namespace Runner;

[TaskName("Package.Web")]
[IsDependentOn(typeof(PublishWebTask))]
/// <summary>
/// Packages the publish output into a zip under `artifacts/packages/WebAPI.Minimal`.
/// - Skips unless BuildContext.PackageArtifacts is true (from `--package`).
/// - Uses BuildContext.BuildConfiguration and BuildContext.Runtime to select the publish directory.
/// - Uses BuildContext.PublishWebRoot and BuildContext.PackagesDir for paths.
/// Args pulled from BuildContext: PackageArtifacts, BuildConfiguration, Runtime, PublishWebRoot, PackagesDir.
/// </summary>
public sealed class PackageWebTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (!context.PackageArtifacts)
        {
            context.Log.Information("Packaging disabled. Enable with --package=true to create a zip.");
            return;
        }
        var configuration = context.BuildConfiguration;
        var runtime = context.Runtime;

        var publishDir = string.IsNullOrWhiteSpace(runtime)
            ? context.PublishWebRoot.Combine(new DirectoryPath(configuration))
            : context.PublishWebRoot.Combine(new DirectoryPath(configuration)).Combine(new DirectoryPath(runtime));

        var packagesDir = context.PackagesDir.Combine("WebAPI.Minimal");
        context.EnsureDirectoryExists(packagesDir);

        var runtimeSuffix = string.IsNullOrWhiteSpace(runtime) ? string.Empty : $"-{runtime}";
        var zipName = $"WebAPI.Minimal-{configuration}{runtimeSuffix}.zip";
        var zipPath = packagesDir.CombineWithFilePath(new FilePath(zipName));

        context.Log.Information($"Packaging {publishDir.FullPath} -> {zipPath.FullPath}");

        // Overwrite any existing zip
        if (System.IO.File.Exists(zipPath.FullPath))
        {
            System.IO.File.Delete(zipPath.FullPath);
        }

        ZipFile.CreateFromDirectory(publishDir.FullPath, zipPath.FullPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        context.Log.Information("Package created.");
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(PackageWebTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
}
