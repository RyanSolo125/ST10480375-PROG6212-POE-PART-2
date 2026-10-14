using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class ResultsControllerTests
{
    [Fact]
    public async Task Capture_AsOwnerOrganiser_AfterEvent_Returns201()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId, daysFromToday: -1);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var enrolment = TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        var controller = TestHelper.Setup(new ResultsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Capture(new ResultCreateDto
        {
            EnrolmentId = enrolment.EnrolmentId, FinishTime = "00:52:10", Position = 3
        });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Results);
        Assert.Equal(new TimeSpan(0, 52, 10), saved.FinishTime);
        Assert.Equal(3, saved.Position);
    }

    [Fact]
    public async Task Capture_AsParticipant_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var controller = TestHelper.Setup(new ResultsController(db), participant.UserId, Roles.Participant);

        var result = await controller.Capture(new ResultCreateDto { EnrolmentId = 1, FinishTime = "00:50:00", Position = 1 });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Capture_InvalidTimeFormat_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var controller = TestHelper.Setup(new ResultsController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.Capture(new ResultCreateDto { EnrolmentId = 1, FinishTime = "fast", Position = 1 });

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task GetMyResults_ReturnsOnlyTheLoggedInParticipantsResults()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var me = TestHelper.AddUser(db, Roles.Participant);
        var someoneElse = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId, daysFromToday: -1);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var myEnrolment = TestHelper.AddEnrolment(db, me.UserId, category.CategoryId);
        var theirEnrolment = TestHelper.AddEnrolment(db, someoneElse.UserId, category.CategoryId);
        db.Results.Add(new Result { EnrolmentId = myEnrolment.EnrolmentId, FinishTime = new TimeSpan(0, 45, 0), Position = 1 });
        db.Results.Add(new Result { EnrolmentId = theirEnrolment.EnrolmentId, FinishTime = new TimeSpan(0, 50, 0), Position = 2 });
        db.SaveChanges();
        var controller = TestHelper.Setup(new ResultsController(db), me.UserId, Roles.Participant);

        var result = await controller.GetMyResults();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<ResultDto>>(ok.Value!);
        var mine = Assert.Single(list);
        Assert.Equal(1, mine.Position);
        Assert.Equal("00:45:00", mine.FinishTime);
    }

    [Fact]
    public async Task GetForEvent_UnknownEvent_Returns404()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new ResultsController(db));

        var result = await controller.GetForEvent(999);

        Assert.Equal(404, TestHelper.StatusOf(result));
    }
}
