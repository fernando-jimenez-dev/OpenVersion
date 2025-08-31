using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion.Abstractions;
using Application.UseCases.ComputeNextVersion.Errors;
using Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework.Entities;
using Application.UseCases.ComputeNextVersion.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.ComputeNextVersion.Infrastructure.EntityFramework;

public class VersionRepository : IVersionRepository
{
    private readonly OpenVersionContext _context;
    private readonly TimeSpan? _testDelay;

    public VersionRepository(OpenVersionContext context, TimeSpan? testDelay = null)
    {
        _context = context;
        _testDelay = testDelay;
    }

    public async Task<Result<IReadOnlyDictionary<string, DomainVersion>>> GetCurrentVersions(CancellationToken cancellationToken = default)
    {
        // Return all versions, grouped by IdentifierName (for the current project)
        var versions = await _context.Versions
            .Where(v => v.ProjectId == 1) // Assuming ProjectId = 1 for now
            .AsNoTracking()
            .Select(v => v.ToDomain())
            .ToDictionaryAsync(v => v.IdentifierName, v => v, cancellationToken);

        return Result<IReadOnlyDictionary<string, DomainVersion>>.Success(versions);
    }

    public async Task<Result> SaveVersion(DomainVersion version, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find by unique constraint: ProjectId + IdentifierName
            var existing = await _context.Versions.FirstOrDefaultAsync(v =>
                    v.ProjectId == version.ProjectId
                    && v.IdentifierName == version.IdentifierName, cancellationToken
                );

            if (existing is null)
            {
                // Insert new version using explicit factory method
                var entity = VersionEntity.CreateFromDomain(version);
                _context.Versions.Add(entity);
            }
            else
            {
                // Update existing version using explicit update method
                existing.UpdateFrom(version);
                _context.Entry(existing).State = EntityState.Modified;
            }

            // Add test delay if specified (for testing concurrency scenarios)
            // This happens after we've loaded the entity but before SaveChanges
            if (_testDelay.HasValue)
            {
                await Task.Delay(_testDelay.Value, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Failure(new VersionConcurrencyError(version, ex));
        }
        catch (Exception ex)
        {
            return Result.Failure(new ApplicationError("RepositoryError", ex.Message, ex));
        }
    }
}