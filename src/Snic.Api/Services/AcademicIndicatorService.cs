using Snic.Api.Contracts;
using Snic.Api.Repositories;

namespace Snic.Api.Services;

public interface IAcademicIndicatorService
{
    Task<AcademicIndicatorsResponse> CalculateAsync(long personId, CancellationToken cancellationToken);
}

public sealed class AcademicIndicatorService(IAcademicIndicatorRepository repository) : IAcademicIndicatorService
{
    public async Task<AcademicIndicatorsResponse> CalculateAsync(long personId, CancellationToken cancellationToken)
    {
        var maximum = await repository.MaximumDaysPastDueAsync(personId, cancellationToken);
        if (maximum is null)
            return new(true, "NO_CREDIT_HISTORY", "Sin historial crediticio; no se calculan indicadores.", null, null, null, null);

        var matches = await repository.MatchingRulesAsync(maximum.Value, cancellationToken);
        if (matches.Count != 1)
            throw new InvalidOperationException("Debe existir una única regla activa para el atraso calculado.");
        var rule = matches[0];
        if (rule.EstimatedDefaultPercentage != 100 - rule.Score)
            throw new InvalidOperationException("La regla de simulación debe cumplir porcentaje = 100 - score.");
        return new(true, "SIMULATED", "Indicadores académicos simulados; no constituyen una evaluación financiera real.",
            maximum, rule.Score, rule.RiskLevel, rule.EstimatedDefaultPercentage);
    }
}
