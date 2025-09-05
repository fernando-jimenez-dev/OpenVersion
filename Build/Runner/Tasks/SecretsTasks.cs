using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Common.IO;
using Cake.Core.IO;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Runner;

[TaskName("Secrets.Materialize")]
/// <summary>
/// Generates a plaintext secrets file from a committed template by replacing ${TOKENS} with values.
/// - Template: BuildContext.SecretsTemplatePath (default: WebAPI secrets.template.json).
/// - Output: BuildContext.SecretsOutDir/BuildContext.SecretsFileName (default: SitePath/secrets.json).
/// - Values source: JSON map file provided via --secretsValuesPath (required).
/// - Fails fast on unresolved tokens.
/// Args pulled from BuildContext: SecretsTemplatePath, SecretsOutDir, SecretsFileName, SecretsValuesPath.
/// </summary>
public sealed class SecretsMaterializeTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var templatePath = context.SecretsTemplatePath;
        var outDir = context.SecretsOutDir;
        var fileName = context.SecretsFileName;

        if (!context.FileExists(templatePath))
        {
            throw new CakeException($"Secrets template not found: {templatePath.FullPath}");
        }
        if (context.SecretsValuesPath == null || !context.FileExists(context.SecretsValuesPath))
        {
            throw new CakeException("--secretsValuesPath is required and must point to an existing JSON file.");
        }

        var template = SecretsUtil.ReadAllText(context, templatePath);
        var valuesMap = SecretsUtil.LoadValuesMap(context, context.SecretsValuesPath);

        var replaced = SecretsUtil.ReplaceTokens(template, valuesMap, out var missing);
        if (missing.Count > 0)
        {
            var names = string.Join(", ", missing);
            throw new CakeException($"Unresolved secret tokens: {names}. Ensure values are provided in the JSON map.");
        }

        context.EnsureDirectoryExists(outDir);
        var outPath = outDir.CombineWithFilePath(new FilePath(fileName));
        var outFile = context.FileSystem.GetFile(outPath);
        using (var stream = outFile.OpenWrite())
        using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, false))
        {
            writer.Write(replaced);
        }
        context.Log.Information($"Secrets written to: {outPath.FullPath}");
    }

}

internal static class SecretsUtil
{
    public static string ReadAllText(ICakeContext context, FilePath path)
    {
        var file = context.FileSystem.GetFile(path);
        using var stream = file.OpenRead();
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, false);
        return reader.ReadToEnd();
    }

    public static System.Collections.Generic.Dictionary<string, string> LoadValuesMap(ICakeContext context, FilePath path)
    {
        var json = ReadAllText(context, path);
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new CakeException("secretsValuesPath must be a flat JSON object mapping TOKEN names to string values.");
            var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                map[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
            return map;
        }
        catch (JsonException ex)
        {
            throw new CakeException($"Failed to parse secretsValuesPath JSON: {ex.Message}");
        }
    }

    public static string ReplaceTokens(string input, System.Collections.Generic.IDictionary<string, string> valuesMap, out System.Collections.Generic.List<string> missing)
    {
        var missingList = new System.Collections.Generic.List<string>();
        string evaluator(Match match)
        {
            var name = match.Groups[1].Value;
            string? value = valuesMap.TryGetValue(name, out var v) ? v : null;
            if (string.IsNullOrEmpty(value))
            {
                if (!missingList.Contains(name)) missingList.Add(name);
                return match.Value; // keep token
            }
            return value;
        }
        var result = Regex.Replace(input, @"\$\{([A-Z0-9_]+)\}", new MatchEvaluator(evaluator));
        missing = missingList;
        return result;
    }
}

[TaskName("Secrets.MaterializeToPublish")]
[IsDependentOn(typeof(PublishWebTask))]
/// <summary>
/// Materializes secrets into the Publish.Web output directory so they are copied by Deploy.IIS.
/// - Output directory is computed from BuildConfiguration/Runtime under BuildContext.PublishWebRoot.
/// - Values source: JSON map file provided via --secretsValuesPath (required).
/// Args pulled from BuildContext: BuildConfiguration, Runtime, PublishWebRoot, SecretsTemplatePath, SecretsFileName, SecretsValuesPath.
/// </summary>
public sealed class SecretsMaterializeToPublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var configuration = context.BuildConfiguration;
        var runtime = context.Runtime;
        var baseOut = context.PublishWebRoot;
        var outDir = string.IsNullOrWhiteSpace(runtime)
            ? baseOut.Combine(new DirectoryPath(configuration))
            : baseOut.Combine(new DirectoryPath(configuration)).Combine(new DirectoryPath(runtime));

        var templatePath = context.SecretsTemplatePath;
        if (!context.FileExists(templatePath))
        {
            throw new CakeException($"Secrets template not found: {templatePath.FullPath}");
        }
        if (context.SecretsValuesPath == null || !context.FileExists(context.SecretsValuesPath))
        {
            throw new CakeException("--secretsValuesPath is required and must point to an existing JSON file.");
        }

        var template = SecretsUtil.ReadAllText(context, templatePath);
        var valuesMap = SecretsUtil.LoadValuesMap(context, context.SecretsValuesPath);
        var replaced = SecretsUtil.ReplaceTokens(template, valuesMap, out var missing);
        if (missing.Count > 0)
        {
            var names = string.Join(", ", missing);
            throw new CakeException($"Unresolved secret tokens: {names}. Ensure values are provided in the JSON map.");
        }

        context.EnsureDirectoryExists(outDir);
        var outPath = outDir.CombineWithFilePath(new FilePath(context.SecretsFileName));
        var pubFile = context.FileSystem.GetFile(outPath);
        using (var stream = pubFile.OpenWrite())
        using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, false))
        {
            writer.Write(replaced);
        }
        context.Log.Information($"Secrets written to publish output: {outPath.FullPath}");
    }
}
