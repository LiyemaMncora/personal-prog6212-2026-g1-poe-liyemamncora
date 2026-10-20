using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class DatabaseSeeder
{
    public const string DemoPassword = "Password123!";

    public static void SeedSampleData(RaceDayDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        const int organiserRoleId = 1;
        const int participantRoleId = 2;

        var thabo = NewUser(organiserRoleId, "Thabo", "Nkosi", "thabo.nkosi@raceday.co.za", "0821234567", new DateOnly(1985, 3, 14));
        var naledi = NewUser(organiserRoleId, "Naledi", "Mokoena", "naledi.mokoena@raceday.co.za", "0839876543", new DateOnly(1990, 7, 22));
        var sipho = NewUser(participantRoleId, "Sipho", "Khumalo", "sipho.khumalo@gmail.com", "0712223333", new DateOnly(1996, 11, 2));
        var chantal = NewUser(participantRoleId, "Chantal", "Pillay", "chantal.pillay@gmail.com", "0734445555", new DateOnly(1993, 5, 30));
        var lerato = NewUser(participantRoleId, "Lerato", "Dlamini", "lerato.dlamini@gmail.com", "0766667777", new DateOnly(2000, 1, 18));

        var durban = new Event
        {
            Organiser = thabo,
            Name = "Durban Beachfront 10km Challenge",
            Description = "A flat and fast race along the Golden Mile promenade. The race starts at uShaka Marine World.",
            EventDate = new DateTime(2026, 8, 16, 6, 0, 0),
            Location = "Durban",
            Province = "KwaZulu-Natal",
            EventType = EventTypes.Running,
            DistanceKm = 10.00m,
            Latitude = -29.867300m,
            Longitude = 31.045400m
        };

        var pietermaritzburg = new Event
        {
            Organiser = thabo,
            Name = "Pietermaritzburg Hill Half Marathon",
            Description = "A hard half marathon through the hills of Pietermaritzburg. Water tables every 3km.",
            EventDate = new DateTime(2026, 11, 8, 5, 30, 0),
            Location = "Pietermaritzburg",
            Province = "KwaZulu-Natal",
            EventType = EventTypes.Running,
            DistanceKm = 21.10m,
            Latitude = -29.601100m,
            Longitude = 30.379400m
        };

        var winelands = new Event
        {
            Organiser = naledi,
            Name = "Cape Winelands Cycle Classic",
            Description = "A cycling event through the wine farms of Paarl. Helmets are compulsory for all riders.",
            EventDate = new DateTime(2026, 12, 6, 7, 0, 0),
            Location = "Paarl",
            Province = "Western Cape",
            EventType = EventTypes.Cycling,
            DistanceKm = 50.00m,
            Latitude = -33.731700m,
            Longitude = 18.962100m
        };

        var durban10 = NewCategory(durban, "10km Run", 10.00m, 120.00m, 800, new TimeSpan(6, 0, 0));
        var durban5 = NewCategory(durban, "5km Fun Run and Walk", 5.00m, 60.00m, 500, new TimeSpan(6, 30, 0));
        var halfMarathon = NewCategory(pietermaritzburg, "21.1km Half Marathon", 21.10m, 180.00m, 1000, new TimeSpan(5, 30, 0));
        NewCategory(pietermaritzburg, "10km Run", 10.00m, 100.00m, 600, new TimeSpan(6, 0, 0));
        NewCategory(winelands, "50km Cycle", 50.00m, 350.00m, 1200, new TimeSpan(7, 0, 0));
        var cycle25 = NewCategory(winelands, "25km Cycle", 25.00m, 200.00m, 900, new TimeSpan(7, 30, 0));

        var enrolment1 = NewEnrolment(durban10, sipho, "D1001");
        var enrolment2 = NewEnrolment(durban5, chantal, "D1002");
        var enrolment3 = NewEnrolment(durban10, lerato, "D1003");
        NewEnrolment(halfMarathon, sipho, "P2001");
        NewEnrolment(cycle25, chantal, "C3001");

        enrolment1.Result = new Result { FinishTime = new TimeSpan(0, 47, 32), PositionOverall = 1, Status = ResultStatuses.Finished };
        enrolment2.Result = new Result { FinishTime = new TimeSpan(0, 31, 5), PositionOverall = 1, Status = ResultStatuses.Finished };
        enrolment3.Result = new Result { FinishTime = new TimeSpan(0, 52, 18), PositionOverall = 2, Status = ResultStatuses.Finished };

        db.Users.AddRange(thabo, naledi, sipho, chantal, lerato);
        db.Events.AddRange(durban, pietermaritzburg, winelands);
        db.SaveChanges();
    }

    private static User NewUser(int roleId, string firstName, string lastName, string email, string phone, DateOnly dateOfBirth)
    {
        return new User
        {
            RoleId = roleId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = PasswordHasher.Hash(DemoPassword),
            PhoneNumber = phone,
            DateOfBirth = dateOfBirth
        };
    }

    private static Category NewCategory(Event ev, string name, decimal distanceKm, decimal entryFee, int maxParticipants, TimeSpan startTime)
    {
        var category = new Category
        {
            Event = ev,
            Name = name,
            DistanceKm = distanceKm,
            EntryFee = entryFee,
            MaxParticipants = maxParticipants,
            StartTime = startTime
        };

        ev.Categories.Add(category);
        return category;
    }

    private static Enrolment NewEnrolment(Category category, User participant, string raceNumber)
    {
        var enrolment = new Enrolment
        {
            Event = category.Event,
            Category = category,
            Participant = participant,
            RaceNumber = raceNumber,
            Status = EnrolmentStatuses.Confirmed
        };

        category.Enrolments.Add(enrolment);
        return enrolment;
    }
}