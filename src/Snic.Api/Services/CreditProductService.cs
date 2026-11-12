using Microsoft.EntityFrameworkCore;
using Snic.Api.Contracts;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public sealed class CreditProductService(
    SnicDbContext dbContext,
    ICreditProductRepository products,
    IPersonRepository persons,
    IInstitutionRepository institutions,
    IUserAccountRepository accounts,
    IAuditService audit) : ICreditProductService
{
    public async Task<IReadOnlyList<CreditProductResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await products.ListAsync(cancellationToken)).Select(Map).ToList();

    public async Task<CreditProductResponse?> FindAsync(long id, CancellationToken cancellationToken)
    {
        var product = await products.FindByIdAsync(id, false, cancellationToken);
        return product is null ? null : Map(product);
    }

    public async Task<ServiceResult<CreditProductResponse>> CreateAsync(CreateCreditProductRequest request, long accountId, CancellationToken cancellationToken)
    {
        var access = await ResolveWriterInstitutionAsync(accountId, request.InstitutionId, cancellationToken);
        if (!access.Succeeded)
            return ServiceResult<CreditProductResponse>.Fail(access.Error!, access.StatusCode);
        var institutionId = access.Value;
        var references = await ResolveReferencesAsync(request.PersonId, institutionId, request.ProductTypeCode, request.ProductStatusCode, cancellationToken);
        if (!references.Succeeded)
            return ServiceResult<CreditProductResponse>.Fail(references.Error!, references.StatusCode);

        var validation = ValidateDates(request.OpeningDate, request.ClosingDate, request.LatestReportDate);
        if (validation is not null)
            return ServiceResult<CreditProductResponse>.Fail(validation, StatusCodes.Status400BadRequest);

        var identifier = request.ExternalProductIdentifier.Trim();
        if (await products.ExternalIdentifierExistsAsync(institutionId, identifier, null, cancellationToken))
            return ServiceResult<CreditProductResponse>.Fail("La institución ya registró un producto con este identificador.", StatusCodes.Status409Conflict);

        var resolved = references.Value!;
        var product = new CreditProduct
        {
            PersonId = resolved.Person.Id,
            InstitutionId = resolved.Institution.Id,
            ProductTypeId = resolved.Type.Id,
            ProductStatusId = resolved.Status.Id,
            ExternalProductIdentifier = identifier,
            ApprovedAmount = request.ApprovedAmount,
            CurrentBalance = request.CurrentBalance,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            OpeningDate = request.OpeningDate,
            ClosingDate = request.ClosingDate,
            LatestReportDate = request.LatestReportDate,
            LatestDaysPastDue = request.LatestDaysPastDue,
            CreatedByAccountId = accountId,
            IsActive = true
        };
        products.Add(product);
        await products.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
        var created = await products.FindByIdAsync(product.Id, false, cancellationToken);
        return ServiceResult<CreditProductResponse>.Ok(Map(created!), StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<CreditProductResponse>> UpdateAsync(long id, UpdateCreditProductRequest request, long accountId, string? ipAddress, CancellationToken cancellationToken)
    {
        var access = await ResolveWriterInstitutionAsync(accountId, request.InstitutionId, cancellationToken);
        if (!access.Succeeded)
            return ServiceResult<CreditProductResponse>.Fail(access.Error!, access.StatusCode);
        var institutionId = access.Value;
        var product = await products.FindByIdAsync(id, true, cancellationToken);
        if (product is null)
            return ServiceResult<CreditProductResponse>.Fail("El producto crediticio no existe.", StatusCodes.Status404NotFound);

        if (product.InstitutionId != institutionId)
            return ServiceResult<CreditProductResponse>.Fail("Solo puedes corregir productos de tu institución.", StatusCodes.Status403Forbidden);

        var references = await ResolveReferencesAsync(request.PersonId, institutionId, request.ProductTypeCode, request.ProductStatusCode, cancellationToken);
        if (!references.Succeeded)
            return ServiceResult<CreditProductResponse>.Fail(references.Error!, references.StatusCode);

        var validation = ValidateDates(request.OpeningDate, request.ClosingDate, request.LatestReportDate);
        if (validation is not null)
            return ServiceResult<CreditProductResponse>.Fail(validation, StatusCodes.Status400BadRequest);

        var identifier = request.ExternalProductIdentifier.Trim();
        if (await products.ExternalIdentifierExistsAsync(institutionId, identifier, id, cancellationToken))
            return ServiceResult<CreditProductResponse>.Fail("La institución ya registró otro producto con este identificador.", StatusCodes.Status409Conflict);

        var oldValues = Snapshot(product);
        var resolved = references.Value!;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        product.PersonId = resolved.Person.Id;
        product.InstitutionId = resolved.Institution.Id;
        product.ProductTypeId = resolved.Type.Id;
        product.ProductStatusId = resolved.Status.Id;
        product.ExternalProductIdentifier = identifier;
        product.ApprovedAmount = request.ApprovedAmount;
        product.CurrentBalance = request.CurrentBalance;
        product.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        product.OpeningDate = request.OpeningDate;
        product.ClosingDate = request.ClosingDate;
        product.LatestReportDate = request.LatestReportDate;
        product.LatestDaysPastDue = request.LatestDaysPastDue;
        product.UpdatedAt = DateTime.UtcNow;
        audit.AddSuccess(accountId, "CREDIT_PRODUCT_UPDATED", "CreditProduct", product.Id, product.PersonId, product.InstitutionId, oldValues, Snapshot(request with { InstitutionId = institutionId }), ipAddress);
        await products.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
        var updated = await products.FindByIdAsync(product.Id, false, cancellationToken);
        return ServiceResult<CreditProductResponse>.Ok(Map(updated!));
    }

    public async Task<ServiceResult<ConsolidatedCreditReportResponse>> GetConsolidatedAsync(string nationalId, CancellationToken cancellationToken)
    {
        if (nationalId.Length != 11 || nationalId.Any(character => !char.IsAsciiDigit(character)))
            return ServiceResult<ConsolidatedCreditReportResponse>.Fail("La cédula debe contener exactamente 11 dígitos.", StatusCodes.Status400BadRequest);

        var person = await persons.FindByNationalIdAsync(nationalId, false, cancellationToken);
        if (person is null)
            return ServiceResult<ConsolidatedCreditReportResponse>.Fail("No existe una persona registrada con esta cédula.", StatusCodes.Status404NotFound);

        var personProducts = (await products.ListByPersonIdAsync(person.Id, cancellationToken)).Select(Map).ToList();
        var summary = new CreditReportSummary(
            personProducts.Count,
            personProducts.Sum(x => x.ApprovedAmount),
            personProducts.Sum(x => x.CurrentBalance));
        return ServiceResult<ConsolidatedCreditReportResponse>.Ok(new ConsolidatedCreditReportResponse(
            PersonService.Map(person), personProducts.Count > 0, summary, personProducts));
    }

    private async Task<ServiceResult<int>> ResolveWriterInstitutionAsync(long accountId, int? requestedInstitutionId, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByIdAsync(accountId, false, cancellationToken);
        if (account is null || !account.IsActive || !account.Role.IsActive ||
            account.Role.Code != "DATA_ENTRY" ||
            account.LockedUntil > DateTime.UtcNow ||
            account.InstitutionId is not int institutionId || account.Institution is not { IsActive: true })
            return ServiceResult<int>.Fail("Se requiere una cuenta DATA_ENTRY con institución activa.", StatusCodes.Status403Forbidden);

        if (requestedInstitutionId is int requested && requested != institutionId)
            return ServiceResult<int>.Fail("No puedes registrar ni trasladar productos a otra institución.", StatusCodes.Status403Forbidden);

        return ServiceResult<int>.Ok(institutionId);
    }

    private async Task<ServiceResult<ResolvedReferences>> ResolveReferencesAsync(long personId, int institutionId, string typeCode, string statusCode, CancellationToken cancellationToken)
    {
        var person = await persons.FindByIdAsync(personId, false, cancellationToken);
        if (person is null || !person.IsActive)
            return ServiceResult<ResolvedReferences>.Fail("La persona no existe o está inactiva.", StatusCodes.Status400BadRequest);

        if (!await institutions.ExistsAsync(institutionId, cancellationToken))
            return ServiceResult<ResolvedReferences>.Fail("La institución no existe o está inactiva.", StatusCodes.Status400BadRequest);
        var institution = await dbContext.Institutions.AsNoTracking().SingleAsync(x => x.Id == institutionId, cancellationToken);

        var type = await products.FindTypeByCodeAsync(typeCode.Trim().ToUpperInvariant(), cancellationToken);
        if (type is null)
            return ServiceResult<ResolvedReferences>.Fail("El tipo de producto no existe o está inactivo.", StatusCodes.Status400BadRequest);

        var status = await products.FindStatusByCodeAsync(statusCode.Trim().ToUpperInvariant(), cancellationToken);
        if (status is null)
            return ServiceResult<ResolvedReferences>.Fail("El estado del producto no existe o está inactivo.", StatusCodes.Status400BadRequest);

        return ServiceResult<ResolvedReferences>.Ok(new ResolvedReferences(person, institution, type, status));
    }

    private static string? ValidateDates(DateOnly openingDate, DateOnly? closingDate, DateOnly? latestReportDate)
    {
        if (closingDate < openingDate)
            return "La fecha de cierre no puede ser anterior a la fecha de apertura.";
        if (latestReportDate < openingDate)
            return "La fecha del último reporte no puede ser anterior a la fecha de apertura.";
        return null;
    }

    internal static CreditProductResponse Map(CreditProduct product) => new(
        product.Id, product.PersonId, product.Person.NationalId,
        product.InstitutionId, product.Institution.LegalName,
        product.ProductType.Code, product.ProductType.Name,
        product.ProductStatus.Code, product.ProductStatus.Name,
        product.ExternalProductIdentifier, product.ApprovedAmount,
        product.CurrentBalance, product.CurrencyCode, product.OpeningDate,
        product.ClosingDate, product.LatestReportDate, product.LatestDaysPastDue,
        product.IsActive, product.CreatedAt, product.UpdatedAt);

    private static object Snapshot(CreditProduct product) => new
    {
        product.PersonId,
        product.InstitutionId,
        ProductTypeCode = product.ProductType.Code,
        ProductStatusCode = product.ProductStatus.Code,
        product.ExternalProductIdentifier,
        product.ApprovedAmount,
        product.CurrentBalance,
        product.CurrencyCode,
        product.OpeningDate,
        product.ClosingDate,
        product.LatestReportDate,
        product.LatestDaysPastDue
    };

    private static object Snapshot(UpdateCreditProductRequest request) => new
    {
        request.PersonId,
        request.InstitutionId,
        ProductTypeCode = request.ProductTypeCode.Trim().ToUpperInvariant(),
        ProductStatusCode = request.ProductStatusCode.Trim().ToUpperInvariant(),
        ExternalProductIdentifier = request.ExternalProductIdentifier.Trim(),
        request.ApprovedAmount,
        request.CurrentBalance,
        CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
        request.OpeningDate,
        request.ClosingDate,
        request.LatestReportDate,
        request.LatestDaysPastDue
    };

    private sealed record ResolvedReferences(Person Person, Institution Institution, ProductType Type, ProductStatus Status);
}
