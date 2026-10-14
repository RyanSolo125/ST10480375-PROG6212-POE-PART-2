using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class EventsControllerTests
{
    private static EventCreateDto ValidEvent() => new EventCreateDto
    {
        EventName = "Durban Night Run",
        Description = "Beachfront night run",
        EventDate = DateTime.Today.AddDays(60),
        Location = "Durban",
        DistanceKm = 10,
        EventType = "Run"
    };

    [Fact]
    public async Task GetAll_NotLoggedIn_Returns200_BecauseEventsArePublic()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db));

        var result = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(ok.Value!);
        Assert.Single(events);
    }

    [Fact]
    public async Task GetById_UnknownEvent_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new EventsController(db));

        var result = await controller.GetById(999);

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Create_AsOrganiser_Returns201_AndSavesEvent()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var controller = TestHelper.Setup(new EventsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Create(ValidEvent());

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Events);
        Assert.Equal("Durban Night Run", saved.EventName);
        Assert.Equal(organiser.UserId, saved.OrganiserId);
    }

    [Fact]
    public async Task Create_AsParticipant_Returns403_AndNothingSaved()
    {
        using var db = TestHelper.CreateDb();
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var controller = TestHelper.Setup(new EventsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Create(ValidEvent());

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Create_NotLoggedIn_Returns401()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new EventsController(db));

        var result = await controller.Create(ValidEvent());

        Assert.Equal(401, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Create_InvalidEventType_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var controller = TestHelper.Setup(new EventsController(db), organiser.UserId, Roles.Organiser);
        var dto = ValidEvent();
        dto.EventType = "Swim";

        var result = await controller.Create(dto);

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Update_AsOwner_Returns200_AndChangesEvent()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db), organiser.UserId, Roles.Organiser);
        var dto = ValidEvent();
        dto.EventName = "Renamed Race";
        dto.EventType = "Walk";

        var result = await controller.Update(ev.EventId, dto);

        Assert.Equal(200, TestHelper.StatusOf(result));
        var saved = await db.Events.FindAsync(ev.EventId);
        Assert.Equal("Renamed Race", saved!.EventName);
        Assert.Equal("Walk", saved.EventType);
    }

    [Fact]
    public async Task Update_AsDifferentOrganiser_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var owner = TestHelper.AddUser(db, Roles.Organiser);
        var otherOrganiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, owner.UserId);
        var controller = TestHelper.Setup(new EventsController(db), otherOrganiser.UserId, Roles.Organiser);

        var result = await controller.Update(ev.EventId, ValidEvent());

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Delete_AsOwner_Returns204_AndRemovesEvent()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(204, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Delete_AsParticipant_Returns403_AndEventStays()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_UnknownEvent_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var controller = TestHelper.Setup(new EventsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Delete(999);

        Assert.Equal(404, TestHelper.StatusOf(result));
    }
}
