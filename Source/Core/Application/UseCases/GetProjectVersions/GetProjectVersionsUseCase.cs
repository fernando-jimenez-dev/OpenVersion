using System;
using System.Linq;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Models;
using Application.UseCases.GetProjectVersions.Models;
using OpenResult;

namespace Application.UseCases.GetProjectVersions;

public class GetProjectVersionsUseCase : IGetProjectVersionsUseCase
{
    private readonly IVersionRepository _versionRepository;

    public GetProjectVersionsUseCase(IVersionRepository versionRepository)
    {
        _versionRepository = versionRepository;
    }

    public async Task<Result<IGetProjectVersionsUseCase.Output>> Run(
        IGetProjectVersionsUseCase.Input input,
        CancellationToken cancellationToken = default)
    {
        var versionsResult = await _versionRepository.GetCurrentVersions(input.ProjectId, cancellationToken);
        if (versionsResult.Failed(out var error))
        {
            return Result<IGetProjectVersionsUseCase.Output>.Failure(error!);
        }

        var domainVersions = versionsResult.Value?.Values ?? Enumerable.Empty<DomainVersion>();

        var versions = domainVersions
            .Select(ProjectVersion.FromDomain)
            .OrderBy(version => version.IdentifierName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<IGetProjectVersionsUseCase.Output>.Success(
            new IGetProjectVersionsUseCase.Output(input.ProjectId, versions));
    }
}
