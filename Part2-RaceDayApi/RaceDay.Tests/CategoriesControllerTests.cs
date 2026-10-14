using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class CategoriesControllerTests
{
    private static CategoryCreateDto NewCategory() => new CategoryCreateDto
    {
        CategoryName = "Under 20", DistanceKm = 10, EntryFee = 100, MaxParticipants = 200
    };

    [Fact]
    public async Task Create_AsOwnerOrganiser_Returns201()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new CategoriesController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Create(ev.EventId, NewCategory());

        Assert.Equal(201, TestHelper.StatusOf(result));
        Assert.Equal(ev.EventId, Assert.Single(db.Categories).EventId);
    }

    [Fact]
    public async Task Create_AsParticipant_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new CategoriesController(db), participant.UserId, Roles.Participant);

        var result = await controller.Create(ev.EventId, NewCategory());

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task GetForEvent_NotLoggedIn_Returns200_BecauseCategoriesArePublic()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Setup(new CategoriesController(db));

        var result = await controller.GetForEvent(ev.EventId);

        Assert.Equal(200, TestHelper.StatusOf(result));
    }
}
