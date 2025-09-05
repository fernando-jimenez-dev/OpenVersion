using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using System;
using System.Diagnostics;

namespace Runner;

// Deploy to local IIS (from Publish.Web output)
[TaskName("Deploy.IIS")]
[IsDependentOn(typeof(SecretsMaterializeToPublishTask))]
/// <summary>
/// Deploys the published Web API to local IIS.
/// - Copies from BuildContext.PublishWebRoot/<Configuration>[/<Runtime>] to BuildContext.SitePath.
/// - Creates/updates IIS Site (BuildContext.SiteName) and App Pool (BuildContext.AppPool), binds to BuildContext.SitePort.
/// - Requires admin rights, IIS + WebAdministration module, and ASP.NET Core Hosting Bundle.
/// Args pulled from BuildContext: BuildConfiguration, Runtime, PublishWebRoot, SiteName, AppPool, SitePath, SitePort.
/// </summary>
public sealed class DeployIisTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var configuration = context.BuildConfiguration;
        var runtime = context.Runtime;

        // Paths
        var baseOut = context.PublishWebRoot;
        var outDir = string.IsNullOrWhiteSpace(runtime)
            ? baseOut.Combine(new DirectoryPath(configuration))
            : baseOut.Combine(new DirectoryPath(configuration)).Combine(new DirectoryPath(runtime));

        // IIS settings (args with sensible defaults)
        var siteName = context.SiteName;
        var appPool = context.AppPool;
        var sitePath = context.SitePath;
        var port = context.SitePort;

        context.Log.Information($"Preparing IIS deploy: Site={siteName}, AppPool={appPool}, Path={sitePath.FullPath}, Port={port}");

        // Copy publish output into IIS physical path
        context.EnsureDirectoryExists(sitePath);
        context.CleanDirectory(sitePath);
        context.CopyDirectory(outDir, sitePath);

        // Apply IIS configuration via PowerShell WebAdministration module
        var script = $@"
$ErrorActionPreference = 'Stop';
Import-Module WebAdministration;
$appPool = '{appPool}';
$site = '{siteName}';
$path = '{sitePath.FullPath.Replace("'", "''")}';
$port = {port};
if (!(Test-Path ""IIS:\AppPools\$appPool"")) {{ New-WebAppPool -Name $appPool | Out-Null }}
Set-ItemProperty ""IIS:\AppPools\$appPool"" -Name managedRuntimeVersion -Value ''
if (!(Test-Path ""IIS:\Sites\$site"")) {{
  New-Website -Name $site -Port $port -PhysicalPath $path -ApplicationPool $appPool | Out-Null
}} else {{
  Set-ItemProperty ""IIS:\Sites\$site"" -Name applicationPool -Value $appPool
  Set-ItemProperty ""IIS:\Sites\$site"" -Name physicalPath -Value $path
}}
Stop-WebAppPool -Name $appPool -ErrorAction SilentlyContinue
Start-WebAppPool -Name $appPool
Stop-Website -Name $site -ErrorAction SilentlyContinue
Start-Website -Name $site
";

        var args = new ProcessArgumentBuilder();
        args.Append("-NoProfile");
        args.Append("-ExecutionPolicy");
        args.Append("Bypass");
        args.Append("-Command");
        args.AppendQuoted(script);

        context.Log.Information("Configuring IIS (requires admin)...");
        // Ensure we invoke 64-bit PowerShell when on a 64-bit OS so WebAdministration writes to the main IIS config
        var psExe = "powershell.exe";
        if (Environment.Is64BitOperatingSystem)
        {
            psExe = Environment.Is64BitProcess
                ? Environment.ExpandEnvironmentVariables(@"%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe")
                : Environment.ExpandEnvironmentVariables(@"%SystemRoot%\\Sysnative\\WindowsPowerShell\\v1.0\\powershell.exe");
        }
        var psi = new ProcessStartInfo(psExe, args.RenderSafe())
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
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            context.Log.Information(stdout);
        }
        if (proc.ExitCode != 0)
        {
            context.Log.Error(stderr);
            throw new CakeException($"IIS configuration failed with exit code {proc.ExitCode}. Run as Administrator and ensure IIS + WebAdministration module are installed.");
        }

        context.Log.Information("Local IIS deploy complete.");
    }
}

// Aggregators
[TaskName("Deploy")]
[IsDependentOn(typeof(DeployIisTask))]
/// <summary>
/// Default deploy orchestrator for IIS. Equivalent to Deploy.IIS.
/// </summary>
public sealed class DeployTask : FrostingTask<BuildContext>
{
}

[TaskName("Deploy.Safe")]
[IsDependentOn(typeof(TestTask))]
[IsDependentOn(typeof(DeployIisTask))]
/// <summary>
/// Deploys to IIS after running tests.
/// - Runs Test orchestrator first (honors `--unit`, `--e2e`, `--all`).
/// - Then runs Deploy.IIS.
/// Args pulled from BuildContext: test flags, and all Deploy.IIS args.
/// </summary>
public sealed class DeploySafeTask : FrostingTask<BuildContext>
{
}
