using Microsoft.EntityFrameworkCore;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;

namespace Snic.Api.Repositories;

public sealed class InstitutionRepository(SnicDbContext dbContext) : IInstitutionRepository
{
    public async Task<IReadOnlyList<Institution>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Institutions.AsNoTracking().Include(x => x.InstitutionType)
            .OrderBy(x => x.LegalName).ToListAsync(cancellationToken);

    public Task<Institution?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Institutions.AsNoTracking().Include(x => x.InstitutionType)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Institutions.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public Task<bool> CodeOrRncExistsAsync(string code, string rnc, CancellationToken cancellationToken) =>
        dbContext.Institutions.AnyAsync(x => x.Code == code || x.Rnc == rnc, cancellationToken);

    public Task<bool> InstitutionTypeExistsAsync(short id, CancellationToken cancellationToken) =>
        dbContext.InstitutionTypes.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public void Add(Institution institution) => dbContext.Institutions.Add(institution);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
