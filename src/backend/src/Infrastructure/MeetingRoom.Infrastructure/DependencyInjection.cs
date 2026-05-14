using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Infrastructure.Repositories;
using MeetingRoom.Infrastructure.Services;

namespace MeetingRoom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Persistence.AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddSingleton<ICalendarProviderFactory, CalendarProviderFactory>();
        services.AddSingleton<Application.Interfaces.ICalendarSyncService, ManualCalendarSyncService>();
        services.AddHostedService<CalendarSyncService>();

        return services;
    }
}
