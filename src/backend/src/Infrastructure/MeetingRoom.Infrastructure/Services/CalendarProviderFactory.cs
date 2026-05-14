using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Interfaces;

namespace MeetingRoom.Infrastructure.Services;

public class CalendarProviderFactory : ICalendarProviderFactory
{
    private readonly Dictionary<CalendarProvider, ICalendarProvider> _providers = new();
    private readonly ILogger<CalendarProviderFactory> _logger;

    public CalendarProviderFactory(ILogger<CalendarProviderFactory> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _providers[CalendarProvider.Google] = new GoogleCalendarProvider();
        _providers[CalendarProvider.Office365] = new Office365CalendarProvider();
        _providers[CalendarProvider.Zoho] = new ZohoCalendarProvider();
        _providers[CalendarProvider.Local] = new LocalCalendarProvider(scopeFactory);
    }

    public ICalendarProvider GetProvider(CalendarProvider provider) =>
        _providers.TryGetValue(provider, out var p)
            ? p
            : throw new ArgumentException($"Unknown calendar provider: {provider}");

    public async Task InitializeProviderAsync(CalendarProvider provider, string credentialsJson, CancellationToken ct = default)
    {
        var p = GetProvider(provider);
        await p.InitializeAsync(credentialsJson, ct);
        _logger.LogInformation("Calendar provider {Provider} initialized", provider);
    }
}
