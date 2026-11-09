using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

/// <summary>Tests for the API features added for the Part 3 MVC front end.</summary>
public class Part3FeatureTests
{
    [Fact]
    public async Task GetMine_AsOrganiser_ReturnsOnlyOwnEvents_WithEnrolmentCount()
    {
        using var db = TestHelper.CreateDb();
        var me = TestHelper.AddUser(db, Roles.Organiser);
        var otherOrganiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var myEvent = TestHelper.AddEvent(db, me.UserId);
        TestHelper.AddEvent(db, otherOrganiser.UserId);
        var category = TestHelper.AddCategory(db, myEvent.EventId);
        TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EventsController(db, new FakeFileStorage()), me.UserId, Roles.Organiser);

        var result = await controller.GetMine();

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(ok.Value!);
        var only = Assert.Single(events);
        Assert.Equal(myEvent.EventId, only.EventId);
        Assert.Equal(1, only.EnrolmentCount);
    }

    [Fact]
    public async Task GetMine_AsParticipant_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var controller = TestHelper.Setup(new EventsController(db, new FakeFileStorage()), participant.UserId, Roles.Participant);

        var result = await controller.GetMine();

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task GetAll_UpcomingFilter_HidesPastEvents()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        TestHelper.AddEvent(db, organiser.UserId, daysFromToday: 10);
        TestHelper.AddEvent(db, organiser.UserId, daysFromToday: -10);
        var controller = TestHelper.Setup(new EventsController(db, new FakeFileStorage()));

        var result = await controller.GetAll(null, null, upcoming: true);

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(ok.Value!);
        Assert.True(Assert.Single(events).EventDate >= DateTime.Today);
    }

    [Fact]
    public async Task UpdateStatus_AsOwnerOrganiser_ConfirmsEnrolment()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var enrolment = TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.UpdateStatus(enrolment.EnrolmentId, new EnrolmentStatusDto { Status = "confirmed" });

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Equal("Confirmed", (await db.Enrolments.FindAsync(enrolment.EnrolmentId))!.Status);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var enrolment = TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.UpdateStatus(enrolment.EnrolmentId, new EnrolmentStatusDto { Status = "Paid" });

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task UpdateStatus_AsParticipant_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var enrolment = TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new EnrolmentsController(db), participant.UserId, Roles.Participant);

        var result = await controller.UpdateStatus(enrolment.EnrolmentId, new EnrolmentStatusDto { Status = "Confirmed" });

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task GetHistory_ShowsPositionOutOfTotalFinishers()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var me = TestHelper.AddUser(db, Roles.Participant);
        var runner2 = TestHelper.AddUser(db, Roles.Participant);
        var runner3 = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId, daysFromToday: -3);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var myEnrolment = TestHelper.AddEnrolment(db, me.UserId, category.CategoryId);
        var enrolment2 = TestHelper.AddEnrolment(db, runner2.UserId, category.CategoryId);
        var enrolment3 = TestHelper.AddEnrolment(db, runner3.UserId, category.CategoryId);
        db.Results.Add(new Result { EnrolmentId = myEnrolment.EnrolmentId, FinishTime = new TimeSpan(0, 50, 0), Position = 2, Status = "Finished" });
        db.Results.Add(new Result { EnrolmentId = enrolment2.EnrolmentId, FinishTime = new TimeSpan(0, 45, 0), Position = 1, Status = "Finished" });
        db.Results.Add(new Result { EnrolmentId = enrolment3.EnrolmentId, Status = "DNF" });   // did not finish - not counted
        db.SaveChanges();
        var controller = TestHelper.Setup(new UsersController(db, new FakeFileStorage()), me.UserId, Roles.Participant);

        var result = await controller.GetHistory();

        var ok = Assert.IsType<OkObjectResult>(result);
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<HistoryDto>>(ok.Value!));
        Assert.Equal(2, item.Position);
        Assert.Equal(2, item.TotalFinishers);   // "2nd out of 2 finishers"
    }
}
