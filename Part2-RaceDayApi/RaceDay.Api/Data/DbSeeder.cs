using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class DbSeeder
{
    /// <summary>
    /// Adds the Part 1 sample data the first time the API runs.
    /// Every sample user's password is: Password123!
    /// </summary>
    public static void Seed(RaceDayDbContext db)
    {
        if (db.Users.Any()) return; // already has data

        string hash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        // Users
        var thandiwe = new User { FullName = "Thandiwe Nkosi", Email = "thandiwe.nkosi@raceday.co.za", PasswordHash = hash, Role = Roles.Organiser, PhoneNumber = "0821234567" };
        var pieter = new User { FullName = "Pieter van Wyk", Email = "pieter.vanwyk@raceday.co.za", PasswordHash = hash, Role = Roles.Organiser, PhoneNumber = "0837654321" };
        var amahle = new User { FullName = "Amahle Dlamini", Email = "amahle.dlamini@gmail.com", PasswordHash = hash, Role = Roles.Participant, PhoneNumber = "0712345678" };
        var johan = new User { FullName = "Johan Botha", Email = "johan.botha@gmail.com", PasswordHash = hash, Role = Roles.Participant, PhoneNumber = "0768889999" };

        // Events
        var cycleTour = new Event { Organiser = thandiwe, EventName = "Cape Town Cycle Tour", Description = "Cycling tour around the Cape Peninsula", EventDate = new DateTime(2026, 3, 8), Location = "Cape Town", DistanceKm = 109, EventType = "Cycle" };
        var twoOceans = new Event { Organiser = thandiwe, EventName = "Two Oceans Marathon", Description = "Marathon along the Cape coastline", EventDate = new DateTime(2026, 4, 4), Location = "Cape Town", DistanceKm = 56, EventType = "Run" };
        var soweto = new Event { Organiser = pieter, EventName = "Soweto Marathon", Description = "Community marathon in Soweto", EventDate = new DateTime(2026, 11, 8), Location = "Soweto", DistanceKm = 42.2m, EventType = "Run" };

        // Categories
        var ride109 = new Category { Event = cycleTour, CategoryName = "Individual Ride 109km", DistanceKm = 109, EntryFee = 950, MaxParticipants = 15000 };
        var ride56 = new Category { Event = cycleTour, CategoryName = "Mini Peloton 56km", DistanceKm = 56, EntryFee = 650, MaxParticipants = 8000 };
        var half21 = new Category { Event = twoOceans, CategoryName = "Half Marathon 21km", DistanceKm = 21.1m, EntryFee = 450, MaxParticipants = 5000 };
        var ultra56 = new Category { Event = twoOceans, CategoryName = "Ultra Marathon 56km", DistanceKm = 56, EntryFee = 650, MaxParticipants = 3000 };
        var fun10 = new Category { Event = soweto, CategoryName = "10km Fun Run", DistanceKm = 10, EntryFee = 200, MaxParticipants = 4000 };
        var full42 = new Category { Event = soweto, CategoryName = "Full Marathon 42.2km", DistanceKm = 42.2m, EntryFee = 500, MaxParticipants = 6000 };

        // Enrolments
        var en1 = new Enrolment { Participant = amahle, Category = ride109 };
        var en2 = new Enrolment { Participant = amahle, Category = half21 };
        var en3 = new Enrolment { Participant = johan, Category = ride56 };
        var en4 = new Enrolment { Participant = johan, Category = fun10 };

        // Results
        var r1 = new Result { Enrolment = en1, FinishTime = new TimeSpan(4, 12, 35), Position = 1245 };
        var r2 = new Result { Enrolment = en3, FinishTime = new TimeSpan(2, 5, 10), Position = 340 };

        // Payments
        var p1 = new Payment { Enrolment = en1, Amount = 950, PaymentMethod = "Card", PaymentStatus = "Paid" };
        var p2 = new Payment { Enrolment = en2, Amount = 450, PaymentMethod = "Card", PaymentStatus = "Paid" };
        var p3 = new Payment { Enrolment = en3, Amount = 650, PaymentMethod = "EFT", PaymentStatus = "Paid" };
        var p4 = new Payment { Enrolment = en4, Amount = 200, PaymentMethod = "Card", PaymentStatus = "Pending" };

        db.AddRange(thandiwe, pieter, amahle, johan,
                    cycleTour, twoOceans, soweto,
                    ride109, ride56, half21, ultra56, fun10, full42,
                    en1, en2, en3, en4,
                    r1, r2,
                    p1, p2, p3, p4);
        db.SaveChanges();
    }
}
