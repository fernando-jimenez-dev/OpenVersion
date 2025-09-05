using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Core.IO;
using System;
using System.Diagnostics;

namespace Runner;

[TaskName("Docker.Build")]
/// <summary>
/// Builds the Docker image for the Web API using the repo Dockerfile.
/// - Uses BuildContext.RepoRoot and Dockerfile at `Source/Presentation/WebAPI.Minimal/Dockerfile`.
/// - Tags image as BuildContext.DockerImage:BuildContext.DockerTag (from `--dockerImage`, `--dockerTag`).
/// Args pulled from BuildContext: RepoRoot, DockerImage, DockerTag.
/// </summary>
public sealed class DockerBuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var repoRoot = context.RepoRoot;
        var dockerfile = repoRoot.CombineWithFilePath(new FilePath("Source/Presentation/WebAPI.Minimal/Dockerfile"));
        var image = context.DockerImage;
        var tag = context.DockerTag;
        var fullTag = $"{image}:{tag}";

        context.Log.Information($"Building Docker image {fullTag} using {dockerfile.FullPath} with context {repoRoot.FullPath}...");

        var args = new ProcessArgumentBuilder();
        args.Append("build");
        args.Append("-f");
        args.AppendQuoted(dockerfile.FullPath);
        args.Append("-t");
        args.AppendQuoted(fullTag);
        args.AppendQuoted(repoRoot.FullPath);

        DockerBuildTask.RunDocker(context, args);
    }

    internal static void RunDocker(BuildContext context, ProcessArgumentBuilder args)
    {
        var psi = new ProcessStartInfo("docker", args.RenderSafe())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) context.Log.Information(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) context.Log.Warning(e.Data); };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            throw new CakeException($"docker command failed with exit code {proc.ExitCode}");
        }
    }
}

[TaskName("Docker.Run")]
[IsDependentOn(typeof(DockerBuildTask))]
/// <summary>
/// Runs the Docker container for the Web API.
/// - Stops/removes any existing container named BuildContext.DockerContainerName (from `--dockerName`).
/// - Maps host port BuildContext.DockerHostPort to container 8080 (from `--dockerPort`).
/// - Sets ASPNETCORE_ENVIRONMENT to BuildContext.DockerEnvironment (from `--dockerEnv`).
/// - Optionally mounts BuildContext.DockerSecretsPath read-only as `/app/secrets.{Env}.json` (from `--dockerSecretsPath`).
/// Args pulled from BuildContext: DockerImage, DockerTag, DockerContainerName, DockerHostPort, DockerEnvironment, DockerSecretsPath.
/// </summary>
public sealed class DockerRunTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var image = context.DockerImage;
        var tag = context.DockerTag;
        var fullTag = $"{image}:{tag}";
        var name = context.DockerContainerName;
        var hostPort = context.DockerHostPort;
        var env = context.DockerEnvironment;
        var secrets = context.DockerSecretsPath;

        // Stop & remove any existing container with same name (best-effort)
        TryRunDocker(context, new [] { "rm", "-f", name });

        var args = new ProcessArgumentBuilder();
        args.Append("run");
        args.Append("-d");
        args.Append("--name");
        args.AppendQuoted(name);
        args.Append("-p");
        args.AppendQuoted($"{hostPort}:8080");
        args.Append("-e");
        args.AppendQuoted($"ASPNETCORE_ENVIRONMENT={env}");
        if (secrets != null)
        {
            var secretsFileName = $"secrets.{env}.json";
            args.Append("-v");
            args.AppendQuoted($"{secrets.FullPath}:/app/{secretsFileName}:ro");
        }
        args.AppendQuoted(fullTag);

        DockerBuildTask.RunDocker(context, args);

        context.Log.Information($"Container '{name}' started. http://localhost:{hostPort}");
    }

    private static void TryRunDocker(BuildContext context, string[] simpleArgs)
    {
        try
        {
            var args = new ProcessArgumentBuilder();
            foreach (var a in simpleArgs) args.Append(a);
            var psi = new ProcessStartInfo("docker", args.RenderSafe())
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit(10000);
        }
        catch { /* ignore */ }
    }
}

[TaskName("Deploy.Docker")]
[IsDependentOn(typeof(DockerRunTask))]
/// <summary>
/// Orchestrates Docker deployment (build + run).
/// Equivalent to running Docker.Build then Docker.Run.
/// </summary>
public sealed class DeployDockerTask : FrostingTask<BuildContext>
{
}
