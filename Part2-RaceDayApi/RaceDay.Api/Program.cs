using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Database - Entity Framework Core (Code-First) with SQL Server
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDB")));

// 2. Session management - keeps the user logged in between requests
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);   // logged out after 30 min of no activity
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;                   // JavaScript can't read the cookie
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllers();

// 3. Swagger - shows every endpoint with its /// description in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for the RaceDay system. Call POST /api/auth/login first - " +
                      "the session cookie is then sent automatically with every request you try here. " +
                      "Sample logins (password Password123!): thandiwe.nkosi@raceday.co.za (Organiser), " +
                      "amahle.dlamini@gmail.com (Participant)."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

// Apply migrations and add sample data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

    if (!db.Database.GetMigrations().Any())
    {
        throw new InvalidOperationException(
            "No migrations found. Open Tools > NuGet Package Manager > Package Manager Console " +
            "and run: Add-Migration InitialCreate   then   Update-Database");
    }

    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseSession();   // must come before MapControllers

app.MapControllers();

app.Run();
