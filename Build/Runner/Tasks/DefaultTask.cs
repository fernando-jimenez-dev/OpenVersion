using Cake.Frosting;

namespace Runner;

[TaskName("Default")]
[IsDependentOn(typeof(CompileTask))]
/// <summary>
/// Default task for quick validation: compiles the solution (Clean -> Restore -> Compile).
/// Args pulled from BuildContext: BuildConfiguration.
/// </summary>
public class DefaultTask : FrostingTask
{
}
