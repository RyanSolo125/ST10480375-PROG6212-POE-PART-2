using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class EnrolmentsControllerTests
{
    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndRecordsParticipantEventAndCategory()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = category.CategoryId });

        Assert.Equal(201, TestHelper.StatusOf(result));

        var saved = await db.Enrolments.Include(e => e.Category).SingleAsync();
        Assert.Equal(participant.UserId, saved.ParticipantId);   // the participant
        Assert.Equal(category.CategoryId, saved.CategoryId);     // the category
        Assert.Equal(ev.EventId, saved.Category!.EventId);       // the event
        Assert.Equal("Confirmed", saved.Status);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = category.CategoryId });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_NotLoggedIn_Returns401()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new EnrolmentsController(db));

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = 1 });

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Enrol_SameEventTwice_Returns409()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = category.CategoryId });

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Equal(1, await db.Enrolments.CountAsync());
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var controller = TestHelper.Setup(new EnrolmentsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = 999 });

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var firstRunner = TestHelper.AddUser(db, Roles.Participant);
        var secondRunner = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId, maxParticipants: 1);
        TestHelper.AddEnrolment(db, firstRunner.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), secondRunner.UserId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = category.CategoryId });

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Enrol_PastEvent_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId, daysFromToday: -5);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentCreateDto { CategoryId = category.CategoryId });

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task GetMyEnrolments_AsParticipant_ReturnsOnlyOwnEnrolments()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var me = TestHelper.AddUser(db, Roles.Participant);
        var someoneElse = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        TestHelper.AddEnrolment(db, me.UserId, category.CategoryId);
        TestHelper.AddEnrolment(db, someoneElse.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), me.UserId, Roles.Participant);

        var result = await controller.GetMyEnrolments();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentDto>>(ok.Value!);
        Assert.Equal(me.UserId, Assert.Single(list).ParticipantId);
    }

    [Fact]
    public async Task GetForEvent_AsOwnerOrganiser_ReturnsEnrolments()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.GetForEvent(ev.EventId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentDto>>(ok.Value!);
        Assert.Equal(participant.UserId, Assert.Single(list).ParticipantId);
    }

    [Fact]
    public async Task GetForEvent_AsParticipant_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), participant.UserId, Roles.Participant);

        var result = await controller.GetForEvent(ev.EventId);

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var owner = TestHelper.AddUser(db, Roles.Participant);
        var otherParticipant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var enrolment = TestHelper.AddEnrolment(db, owner.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), otherParticipant.UserId, Roles.Participant);

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Single(db.Enrolments);
    }
}
