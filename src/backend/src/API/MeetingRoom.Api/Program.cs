using MeetingRoom.Infrastructure;
using MeetingRoom.Infrastructure.Persistence;
using MeetingRoom.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Meeting Room Display API", Version = "v1" });
});
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddScoped<MeetingRoom.Application.UseCases.Rooms.GetRooms>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Rooms.GetRoomById>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Rooms.CreateRoom>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Rooms.UpdateRoom>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Rooms.DeleteRoom>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Status.GetAllRoomStatuses>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Status.GetRoomStatus>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Reservation.QuickReserve>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Calendar.SyncCalendars>();
builder.Services.AddScoped<MeetingRoom.Application.UseCases.Calendar.ConfigureProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
