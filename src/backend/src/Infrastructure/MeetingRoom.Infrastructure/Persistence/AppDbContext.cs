using Microsoft.EntityFrameworkCore;
using MeetingRoom.Core.Domain.Entities;

namespace MeetingRoom.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<LocalMeeting> LocalMeetings => Set<LocalMeeting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalMeeting>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.RoomId).IsRequired().HasMaxLength(8);
            b.Property(m => m.Title).IsRequired().HasMaxLength(200);
            b.Property(m => m.Organizer).IsRequired().HasMaxLength(100);
            b.HasIndex(m => m.CalendarId);
            b.HasIndex(m => m.RoomId);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
