using Microsoft.EntityFrameworkCore;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;

namespace Snic.Api.Repositories;

public sealed class PersonRepository(SnicDbContext dbContext) : IPersonRepository
{
    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Persons.AsNoTracking().OrderBy(x => x.FirstSurname).ThenBy(x => x.FirstName).ToListAsync(cancellationToken);

    public Task<Person?> FindByIdAsync(long id, bool tracked, CancellationToken cancellationToken) =>
        Query(tracked).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Person?> FindByNationalIdAsync(string nationalId, bool tracked, CancellationToken cancellationToken) =>
        Query(tracked).SingleOrDefaultAsync(x => x.NationalId == nationalId, cancellationToken);

    public Task<bool> NationalIdExistsAsync(string nationalId, long? excludingId, CancellationToken cancellationToken) =>
        dbContext.Persons.AnyAsync(x => x.NationalId == nationalId && (!excludingId.HasValue || x.Id != excludingId), cancellationToken);

    public void Add(Person person) => dbContext.Persons.Add(person);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Person> Query(bool tracked) => tracked ? dbContext.Persons : dbContext.Persons.AsNoTracking();
}
