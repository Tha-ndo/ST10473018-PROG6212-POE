using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static void Seed(RaceDayDbContext db, IPasswordHasher<AppUser> hasher)
    {
        if (db.Users.Any() || db.Venues.Any()) return;

        AppUser MakeUser(string name, string email, string role, string phone)
        {
            var user = new AppUser { FullName = name, Email = email, Role = role, PhoneNumber = phone };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        var thandiwe = MakeUser("Thandiwe Mokoena", "thandiwe@comrades.org.za", Roles.Organiser, "0821234567");
        var pieter = MakeUser("Pieter van der Merwe", "pieter@capetowncycle.co.za", Roles.Organiser, "0839876543");
        var sipho = MakeUser("Sipho Nkosi", "sipho.nkosi@example.com", Roles.Participant, "0731112222");
        var amanda = MakeUser("Amanda Botha", "amanda.botha@example.com", Roles.Participant, "0725556666");

        var comrades = new Venue { Name = "Comrades Marathon Route", AddressLine = "Pietermaritzburg CBD", City = "Pietermaritzburg", Province = "KwaZulu-Natal", Latitude = -29.6002m, Longitude = 30.3792m };
        var waterfront = new Venue { Name = "V&A Waterfront", AddressLine = "Dock Road", City = "Cape Town", Province = "Western Cape", Latitude = -33.9031m, Longitude = 18.4201m };
        var soweto = new Venue { Name = "Soweto Athletics Stadium", AddressLine = "Chris Hani Road", City = "Soweto", Province = "Gauteng", Latitude = -26.2679m, Longitude = 27.8585m };

        var events = new[]
        {
            new RaceEvent
            {
                Organiser = thandiwe, Venue = comrades, Title = "Comrades Marathon 2027",
                Description = "Iconic ultramarathon between Pietermaritzburg and Durban.",
                EventDate = new DateOnly(2027, 6, 13), EventType = "Running",
                Categories =
                {
                    new Category { Name = "Down Run (Full)", DistanceKm = 90m, EntryFee = 950m, MaxParticipants = 20000 },
                    new Category { Name = "Novice 10km", DistanceKm = 10m, EntryFee = 250m, MaxParticipants = 2000 },
                }
            },
            new RaceEvent
            {
                Organiser = pieter, Venue = waterfront, Title = "Cape Town Cycle Tour 2027",
                Description = "Scenic cycling tour around the Cape Peninsula.",
                EventDate = new DateOnly(2027, 3, 8), EventType = "Cycling",
                Categories =
                {
                    new Category { Name = "109km Race", DistanceKm = 109m, EntryFee = 550m, MaxParticipants = 35000 },
                    new Category { Name = "35km Mini Cycle", DistanceKm = 35m, EntryFee = 350m, MaxParticipants = 5000 },
                }
            },
            new RaceEvent
            {
                Organiser = thandiwe, Venue = soweto, Title = "Soweto Marathon 2027",
                Description = "Community road running event through Soweto.",
                EventDate = new DateOnly(2027, 11, 7), EventType = "Running",
                Categories =
                {
                    new Category { Name = "Half Marathon", DistanceKm = 21.1m, EntryFee = 300m, MaxParticipants = 8000 },
                    new Category { Name = "5km Fun Walk", DistanceKm = 5m, EntryFee = 100m, MaxParticipants = 3000 },
                }
            },
        };

        db.Users.AddRange(sipho, amanda);
        db.Events.AddRange(events);
        db.SaveChanges();
    }
}
