using Snic.Api.Contracts;
using Snic.Api.Domain;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public sealed class InstitutionService(IInstitutionRepository institutions) : IInstitutionService
{
    public async Task<IReadOnlyList<InstitutionResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await institutions.ListAsync(cancellationToken)).Select(Map).ToList();

    public async Task<InstitutionResponse?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var institution = await institutions.FindByIdAsync(id, cancellationToken);
        return institution is null ? null : Map(institution);
    }

    public async Task<ServiceResult<InstitutionResponse>> CreateAsync(CreateInstitutionRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (!await institutions.InstitutionTypeExistsAsync(request.InstitutionTypeId, cancellationToken))
            return ServiceResult<InstitutionResponse>.Fail("El tipo de institución no existe o está inactivo.", StatusCodes.Status400BadRequest);
        if (await institutions.CodeOrRncExistsAsync(code, request.Rnc, cancellationToken))
            return ServiceResult<InstitutionResponse>.Fail("El código o RNC ya está registrado.", StatusCodes.Status409Conflict);

        var institution = new Institution
        {
            InstitutionTypeId = request.InstitutionTypeId,
            Code = code,
            Rnc = request.Rnc,
            LegalName = request.LegalName.Trim(),
            CommercialName = request.CommercialName?.Trim(),
            IsActive = true
        };
        institutions.Add(institution);
        await institutions.SaveChangesAsync(cancellationToken);
        var created = await institutions.FindByIdAsync(institution.Id, cancellationToken);
        return ServiceResult<InstitutionResponse>.Ok(Map(created!), StatusCodes.Status201Created);
    }

    private static InstitutionResponse Map(Institution x) => new(
        x.Id, x.InstitutionTypeId, x.InstitutionType.Name, x.Code, x.Rnc,
        x.LegalName, x.CommercialName, x.IsActive, x.CreatedAt);
}
