using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task GetProfile_NotLoggedIn_Returns401()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new UsersController(db));

        var result = await controller.GetProfile();

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task GetProfile_LoggedIn_ReturnsOwnProfile()
    {
        using var db = TestHelper.CreateDb();
        var user = TestHelper.AddUser(db, Roles.Organiser, "me@test.com");
        var controller = TestHelper.Setup(new UsersController(db), user.UserId, Roles.Organiser);

        var result = await controller.GetProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserDto>(ok.Value);
        Assert.Equal("me@test.com", profile.Email);
    }

    [Fact]
    public async Task UpdateProfile_LoggedIn_Returns200_AndSavesChanges()
    {
        using var db = TestHelper.CreateDb();
        var user = TestHelper.AddUser(db, Roles.Participant);
        var controller = TestHelper.Setup(new UsersController(db), user.UserId, Roles.Participant);

        var result = await controller.UpdateProfile(new UpdateProfileDto { FullName = "Updated Name", PhoneNumber = "0820000000" });

        Assert.Equal(200, TestHelper.StatusOf(result));
        var saved = await db.Users.FindAsync(user.UserId);
        Assert.Equal("Updated Name", saved!.FullName);
        Assert.Equal("0820000000", saved.PhoneNumber);
    }

    [Fact]
    public async Task GetHistory_AsParticipant_ReturnsEnrolmentWithResult()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId, daysFromToday: -2);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var enrolment = TestHelper.AddEnrolment(db, participant.UserId, category.CategoryId);
        db.Results.Add(new Result { EnrolmentId = enrolment.EnrolmentId, FinishTime = new TimeSpan(1, 2, 3), Position = 7 });
        db.SaveChanges();
        var controller = TestHelper.Setup(new UsersController(db), participant.UserId, Roles.Participant);

        var result = await controller.GetHistory();

        var ok = Assert.IsType<OkObjectResult>(result);
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<HistoryDto>>(ok.Value!));
        Assert.Equal("Test Race", item.EventName);
        Assert.Equal("01:02:03", item.FinishTime);
        Assert.Equal(7, item.Position);
    }

    [Fact]
    public async Task GetHistory_AsOrganiser_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var controller = TestHelper.Setup(new UsersController(db), organiser.UserId, Roles.Organiser);

        var result = await controller.GetHistory();

        Assert.Equal(403, TestHelper.StatusOf(result));
    }
}
