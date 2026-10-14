using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ValidParticipant_Returns201_AndHashesPassword()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Register(new RegisterDto
        {
            FullName = "New Runner",
            Email = "runner@test.com",
            Password = "Password123!",
            Role = "Participant"
        });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = await db.Users.SingleAsync();
        Assert.Equal(Roles.Participant, saved.Role);
        Assert.NotEqual("Password123!", saved.PasswordHash);                 // not stored as plain text
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", saved.PasswordHash));
    }

    [Fact]
    public async Task Register_LowercaseOrganiserRole_IsSavedAsOrganiser()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Register(new RegisterDto
        {
            FullName = "Event Boss", Email = "boss@test.com", Password = "Password123!", Role = "organiser"
        });

        Assert.Equal(201, TestHelper.StatusOf(result));
        Assert.Equal(Roles.Organiser, (await db.Users.SingleAsync()).Role);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Register(new RegisterDto
        {
            FullName = "Hacker", Email = "admin@test.com", Password = "Password123!", Role = "Admin"
        });

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var db = TestHelper.CreateDb();
        TestHelper.AddUser(db, Roles.Participant, "taken@test.com");
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Register(new RegisterDto
        {
            FullName = "Copy Cat", Email = "taken@test.com", Password = "Password123!", Role = "Participant"
        });

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_WithToken_AndStoresUserInSession()
    {
        using var db = TestHelper.CreateDb();
        var user = TestHelper.AddUser(db, Roles.Organiser, "login@test.com");
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Login(new LoginDto { Email = "login@test.com", Password = "Password123!" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.Token));
        Assert.Equal(user.UserId, body.UserId);
        Assert.Equal(Roles.Organiser, body.Role);

        Assert.Equal(user.UserId, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Organiser, controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401_AndNoSession()
    {
        using var db = TestHelper.CreateDb();
        TestHelper.AddUser(db, Roles.Participant, "login@test.com");
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Login(new LoginDto { Email = "login@test.com", Password = "WrongPassword" });

        Assert.Equal(401, TestHelper.StatusOf(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new AuthController(db));

        var result = await controller.Login(new LoginDto { Email = "nobody@test.com", Password = "Password123!" });

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new AuthController(db), userId: 1, role: Roles.Participant);

        var result = controller.Logout();

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }
}
