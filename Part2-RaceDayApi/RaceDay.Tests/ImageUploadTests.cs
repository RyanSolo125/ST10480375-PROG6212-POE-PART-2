using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Tests;

public class ImageUploadTests
{
    // Builds a fake uploaded file, like the one the MVC app sends
    private static IFormFile MakeFile(string fileName, string contentType = "image/png", int sizeInBytes = 100)
    {
        var bytes = new byte[sizeInBytes];
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task UploadBanner_AsOwner_StoresImage_AndSavesFileName()
    {
        using var db = TestHelper.CreateDb();
        var storage = new FakeFileStorage();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db, storage), organiser.UserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile("banner.png"));

        Assert.Equal(200, TestHelper.StatusOf(result));
        var saved = await db.Events.FindAsync(ev.EventId);
        Assert.NotNull(saved!.BannerFileName);
        Assert.True(storage.Files.ContainsKey(saved.BannerFileName!));   // the image went to storage
    }

    [Fact]
    public async Task UploadBanner_ReplacingBanner_DeletesOldImage()
    {
        using var db = TestHelper.CreateDb();
        var storage = new FakeFileStorage();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db, storage), organiser.UserId, Roles.Organiser);

        await controller.UploadBanner(ev.EventId, MakeFile("first.png"));
        string firstBanner = (await db.Events.FindAsync(ev.EventId))!.BannerFileName!;

        await controller.UploadBanner(ev.EventId, MakeFile("second.jpg", "image/jpeg"));

        Assert.Contains(firstBanner, storage.DeletedFiles);
        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_NotAnImage_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var storage = new FakeFileStorage();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db, storage), organiser.UserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile("virus.exe", "application/octet-stream"));

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_TooLarge_Returns400()
    {
        using var db = TestHelper.CreateDb();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db, new FakeFileStorage()), organiser.UserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile("huge.png", sizeInBytes: 6 * 1024 * 1024));

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_Returns403()
    {
        using var db = TestHelper.CreateDb();
        var storage = new FakeFileStorage();
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db, storage), participant.UserId, Roles.Participant);

        var result = await controller.UploadBanner(ev.EventId, MakeFile("banner.png"));

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_StorageNotConfigured_Returns503()
    {
        using var db = TestHelper.CreateDb();
        var storage = new FakeFileStorage { IsConfigured = false };
        var organiser = TestHelper.AddUser(db, Roles.Organiser);
        var ev = TestHelper.AddEvent(db, organiser.UserId);
        var controller = TestHelper.Setup(new EventsController(db, storage), organiser.UserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile("banner.png"));

        Assert.Equal(503, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task UploadProfilePicture_LoggedIn_SavesFileName()
    {
        using var db = TestHelper.CreateDb();
        var storage = new FakeFileStorage();
        var participant = TestHelper.AddUser(db, Roles.Participant);
        var controller = TestHelper.Setup(new UsersController(db, storage), participant.UserId, Roles.Participant);

        var result = await controller.UploadProfilePicture(MakeFile("me.jpg", "image/jpeg"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserDto>(ok.Value);
        Assert.NotNull(profile.ProfilePictureFileName);
        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task UploadProfilePicture_NotLoggedIn_Returns401()
    {
        using var db = TestHelper.CreateDb();
        var controller = TestHelper.Setup(new UsersController(db, new FakeFileStorage()));

        var result = await controller.UploadProfilePicture(MakeFile("me.jpg", "image/jpeg"));

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task GetFile_ExistingImage_ReturnsFile()
    {
        var storage = new FakeFileStorage();
        string fileName = await storage.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes("img")), "image/png", ".png");
        var controller = TestHelper.Setup(new FilesController(storage));

        var result = await controller.Get(fileName);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task GetFile_UnsafeName_Returns404()
    {
        var controller = TestHelper.Setup(new FilesController(new FakeFileStorage()));

        var result = await controller.Get("..\\appsettings.json");

        Assert.Equal(404, TestHelper.StatusOf(result));
    }
}
