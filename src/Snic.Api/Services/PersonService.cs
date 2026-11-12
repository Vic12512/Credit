using Microsoft.EntityFrameworkCore;
using Snic.Api.Contracts;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public sealed class PersonService(
    SnicDbContext dbContext,
    IPersonRepository persons,
    IAuditService audit) : IPersonService
{
    public async Task<IReadOnlyList<PersonResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await persons.ListAsync(cancellationToken)).Select(Map).ToList();

    public async Task<PersonResponse?> FindAsync(long id, CancellationToken cancellationToken)
    {
        var person = await persons.FindByIdAsync(id, false, cancellationToken);
        return person is null ? null : Map(person);
    }

    public async Task<ServiceResult<PersonResponse>> CreateAsync(CreatePersonRequest request, long accountId, CancellationToken cancellationToken)
    {
        var validation = ValidateBirthDate(request.BirthDate);
        if (validation is not null)
            return ServiceResult<PersonResponse>.Fail(validation, StatusCodes.Status400BadRequest);

        var nationalId = request.NationalId.Trim();
        if (await persons.NationalIdExistsAsync(nationalId, null, cancellationToken))
            return ServiceResult<PersonResponse>.Fail("Ya existe una persona registrada con esta cédula.", StatusCodes.Status409Conflict);

        var person = new Person
        {
            NationalId = nationalId,
            FirstName = request.FirstName.Trim(),
            MiddleName = Clean(request.MiddleName),
            FirstSurname = request.FirstSurname.Trim(),
            SecondSurname = Clean(request.SecondSurname),
            BirthDate = request.BirthDate,
            CreatedByAccountId = accountId,
            IsActive = true
        };
        persons.Add(person);
        await persons.SaveChangesAsync(cancellationToken);
        return ServiceResult<PersonResponse>.Ok(Map(person), StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<PersonResponse>> UpdateAsync(long id, UpdatePersonRequest request, long accountId, string? ipAddress, CancellationToken cancellationToken)
    {
        var validation = ValidateBirthDate(request.BirthDate);
        if (validation is not null)
            return ServiceResult<PersonResponse>.Fail(validation, StatusCodes.Status400BadRequest);

        var person = await persons.FindByIdAsync(id, true, cancellationToken);
        if (person is null)
            return ServiceResult<PersonResponse>.Fail("La persona no existe.", StatusCodes.Status404NotFound);

        var nationalId = request.NationalId.Trim();
        if (await persons.NationalIdExistsAsync(nationalId, id, cancellationToken))
            return ServiceResult<PersonResponse>.Fail("Ya existe otra persona registrada con esta cédula.", StatusCodes.Status409Conflict);

        var oldValues = Snapshot(person);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        person.NationalId = nationalId;
        person.FirstName = request.FirstName.Trim();
        person.MiddleName = Clean(request.MiddleName);
        person.FirstSurname = request.FirstSurname.Trim();
        person.SecondSurname = Clean(request.SecondSurname);
        person.BirthDate = request.BirthDate;
        person.UpdatedAt = DateTime.UtcNow;
        audit.AddSuccess(accountId, "PERSON_UPDATED", "Person", person.Id, person.Id, null, oldValues, Snapshot(person), ipAddress);
        await persons.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ServiceResult<PersonResponse>.Ok(Map(person));
    }

    internal static PersonResponse Map(Person person) => new(
        person.Id, person.NationalId, person.FirstName, person.MiddleName,
        person.FirstSurname, person.SecondSurname, person.BirthDate,
        person.IsActive, person.CreatedAt, person.UpdatedAt);

    private static object Snapshot(Person person) => new
    {
        person.NationalId,
        person.FirstName,
        person.MiddleName,
        person.FirstSurname,
        person.SecondSurname,
        person.BirthDate
    };

    private static string? ValidateBirthDate(DateOnly? birthDate) =>
        birthDate > DateOnly.FromDateTime(DateTime.UtcNow) ? "La fecha de nacimiento no puede estar en el futuro." : null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
