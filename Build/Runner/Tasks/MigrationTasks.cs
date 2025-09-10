using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.DotNet.Run;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using System.IO.Compression;
using System.Diagnostics;
using System;
using System.Linq;

namespace Runner;

[TaskName("Publish.Migrator")]
public sealed class PublishMigratorTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var configuration = context.BuildConfiguration;
        var migratorProject = context.RepoRoot.CombineWithFilePath(new FilePath("Database/Migrator/Migrator.csproj"));
        var outDir = context.PublishRoot.Combine("Migrator").Combine(new DirectoryPath(configuration));

        context.Log.Information($"Publishing migrator {migratorProject.FullPath} -> {outDir.FullPath} ({configuration})...");
        context.EnsureDirectoryExists(outDir);

        var settings = new DotNetPublishSettings
        {
            Configuration = configuration,
            OutputDirectory = outDir,
            NoBuild = false
        };

        context.DotNetPublish(migratorProject.FullPath, settings);
        context.Log.Information("Publish.Migrator complete.");
    }
}

[TaskName("Package.Migrator")]
[IsDependentOn(typeof(PublishMigratorTask))]
public sealed class PackageMigratorTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (!context.PackageArtifacts)
        {
            context.Log.Information("Packaging disabled. Enable with --package=true to create migrator zip.");
            return;
        }

        var configuration = context.BuildConfiguration;
        var publishDir = context.PublishRoot.Combine("Migrator").Combine(new DirectoryPath(configuration));

        var packagesDir = context.PackagesDir.Combine("Migrator");
        context.EnsureDirectoryExists(packagesDir);

        var zipName = $"Migrator-{configuration}.zip";
        var zipPath = packagesDir.CombineWithFilePath(new FilePath(zipName));

        context.Log.Information($"Packaging {publishDir.FullPath} -> {zipPath.FullPath}");

        if (System.IO.File.Exists(zipPath.FullPath))
        {
            System.IO.File.Delete(zipPath.FullPath);
        }

        ZipFile.CreateFromDirectory(publishDir.FullPath, zipPath.FullPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        context.Log.Information("Package.Migrator created.");
    }
}

[TaskName("Migrate.Db")]
public sealed class MigrateDbTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // Validate required pieces
        string Require(string? v, string name)
        {
            if (string.IsNullOrWhiteSpace(v))
                throw new CakeException($"--{name} is required for Migrate.Db");
            return v!;
        }

        var host = Require(context.DbHost, "dbHost");
        var port = Require(context.DbPort, "dbPort");
        var db = Require(context.DbName, "dbName");
        var user = Require(context.DbUser, "dbUser");
        var pwd = Require(context.DbPassword, "dbPassword");
        var ssl = Require(context.DbSslMode, "dbSslMode");

        var conn = $"Host={host};Port={port};Database={db};Username={user};Password={pwd};SSL Mode={ssl}";

        var configuration = context.BuildConfiguration;

        if (!string.IsNullOrWhiteSpace(context.MigratorPath))
        {
            var dllPath = ResolveMigratorDllPath(context, context.MigratorPath!);
            context.Log.Information("Running published migrator: {0}", dllPath.FullPath);

            var args = new ProcessArgumentBuilder();
            args.AppendQuoted(dllPath.FullPath);
            args.Append("--conn");
            args.AppendQuoted(conn);
            if (context.DbRecreate)
                args.Append("--recreate");
            if (context.DbTargetMigration.HasValue)
            {
                args.Append("--target");
                args.Append(context.DbTargetMigration.Value.ToString());
            }

            var psi = new ProcessStartInfo("dotnet", args.RenderSafe())
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi)!;
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (!string.IsNullOrWhiteSpace(stdout)) context.Log.Information(stdout);
            if (proc.ExitCode != 0)
            {
                context.Log.Error(stderr);
                throw new CakeException($"Migrator exited with code {proc.ExitCode}");
            }
        }
        else
        {
            var migratorProject = context.RepoRoot.CombineWithFilePath(new FilePath("Database/Migrator/Migrator.csproj"));
            context.Log.Information("Running DB migrations from project: {0}", migratorProject.FullPath);

            var runSettings = new DotNetRunSettings
            {
                Configuration = configuration,
                NoBuild = false
            };
            runSettings.ArgumentCustomization = args =>
            {
                args.Append("--");
                args.Append("--conn");
                args.AppendQuoted(conn);
                if (context.DbRecreate)
                    args.Append("--recreate");
                if (context.DbTargetMigration.HasValue)
                {
                    args.Append("--target");
                    args.Append(context.DbTargetMigration.Value.ToString());
                }
                return args;
            };

            context.DotNetRun(migratorProject.FullPath, runSettings);
        }

        context.Log.Information("Migrations completed successfully.");
    }

    private FilePath ResolveMigratorDllPath(ICakeContext context, string rawPath)
    {
        // Normalize to absolute path
        var abs = System.IO.Path.IsPathRooted(rawPath)
            ? rawPath
            : context.Environment.WorkingDirectory.Combine(rawPath).FullPath;

        if (System.IO.File.Exists(abs))
        {
            if (!abs.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                throw new CakeException($"migratorPath points to a file that is not a .dll: {abs}");
            return new FilePath(abs);
        }

        if (System.IO.Directory.Exists(abs))
        {
            var candidate = System.IO.Path.Combine(abs, "Migrator.dll");
            if (System.IO.File.Exists(candidate))
                return new FilePath(candidate);

            // Fallback: first *.dll that looks like migrator
            var first = System.IO.Directory.GetFiles(abs, "*.dll").FirstOrDefault(p => System.IO.Path.GetFileName(p).StartsWith("Migrator", StringComparison.OrdinalIgnoreCase))
                        ?? System.IO.Directory.GetFiles(abs, "*.dll").FirstOrDefault();
            if (first != null)
                return new FilePath(first);

            throw new CakeException($"No .dll found in migrator directory: {abs}");
        }

        throw new CakeException($"migratorPath not found: {abs}");
    }
}