using FieldFlow.Application.Abstractions;
using FieldFlow.Domain.Calendar;
using FieldFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FieldFlow.Infrastructure.Calendar;

/// <summary>
/// Monta o <see cref="SlaCalculator"/> a partir da configuração + tabela <c>holidays</c>.
/// Os feriados ficam em <see cref="IMemoryCache"/> para não consultar o banco a cada OS.
/// </summary>
internal sealed class BusinessCalendarProvider : IBusinessCalendarProvider
{
    private const string CacheKey = "fieldflow:business-calendar";

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BusinessCalendarOptions _options;

    public BusinessCalendarProvider(IMemoryCache cache, IServiceScopeFactory scopeFactory, IOptions<BusinessCalendarOptions> options)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    public async Task<SlaCalculator> GetSlaCalculatorAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out SlaCalculator? cached) && cached is not null)
            return cached;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var holidays = await context.Holidays.AsNoTracking().Select(h => h.Date).ToListAsync(cancellationToken);

        var calendar = new BusinessCalendar(
            TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId),
            _options.WorkdayStart,
            _options.WorkdayEnd,
            WorkingDaysExtensions.FromDays(_options.WorkingDays),
            holidays);

        var calculator = new SlaCalculator(calendar);
        _cache.Set(CacheKey, calculator, _options.CacheDuration);
        return calculator;
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}
