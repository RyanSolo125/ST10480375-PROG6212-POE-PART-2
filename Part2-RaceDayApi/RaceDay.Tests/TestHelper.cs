using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public static class TestHelper
{
    /// <summary>A brand-new, empty in-memory database for each test.</summary>
    public static RaceDayDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RaceDayDbContext(options);
    }

    /// <summary>
    /// Gives the controller a session. Leave userId and role null for a "not logged in" request.
    /// </summary>
    public static T Setup<T>(T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new FakeSession();

        if (userId != null) httpContext.Session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role != null) httpContext.Session.SetString(SessionKeys.Role, role);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    /// <summary>Gets the HTTP status code from any controller result.</summary>
    public static int StatusOf(IActionResult result)
    {
        if (result is ObjectResult objectResult) return objectResult.StatusCode ?? 200;
        if (result is StatusCodeResult statusResult) return statusResult.StatusCode;
        throw new Exception("Unknown result type: " + result.GetType().Name);
    }

    public static User AddUser(RaceDayDbContext db, string role, string? email = null)
    {
        var user = new User
        {
            FullName = role + " Tester",
            Email = email ?? $"{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 4), // low work factor = faster tests
            Role = role
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(RaceDayDbContext db, int organiserId, int daysFromToday = 30)
    {
        var ev = new Event
        {
            OrganiserId = organiserId,
            EventName = "Test Race",
            Description = "A test event",
            EventDate = DateTime.Today.AddDays(daysFromToday),
            Location = "Cape Town",
            DistanceKm = 10,
            EventType = "Run"
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(RaceDayDbContext db, int eventId, int? maxParticipants = null)
    {
        var category = new Category
        {
            EventId = eventId,
            CategoryName = "Senior 10km",
            DistanceKm = 10,
            EntryFee = 150,
            MaxParticipants = maxParticipants
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(RaceDayDbContext db, int participantId, int categoryId)
    {
        var enrolment = new Enrolment { ParticipantId = participantId, CategoryId = categoryId };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }
}
