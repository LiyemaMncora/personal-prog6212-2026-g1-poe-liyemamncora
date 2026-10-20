using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", t =>
            {
                t.HasCheckConstraint("CK_Roles_roleName", "[roleName] IN ('Organiser', 'Participant')");
            });

            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleId).HasColumnName("roleID");
            entity.Property(r => r.RoleName).HasColumnName("roleName").HasMaxLength(20).IsRequired();

            entity.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UQ_Roles_roleName");

            entity.HasData(
                new Role { RoleId = 1, RoleName = RoleNames.Organiser },
                new Role { RoleId = 2, RoleName = RoleNames.Participant });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).HasColumnName("userID");
            entity.Property(u => u.RoleId).HasColumnName("roleID");
            entity.Property(u => u.FirstName).HasColumnName("firstName").HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasColumnName("lastName").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(u => u.PasswordHash).HasColumnName("passwordHash").HasMaxLength(255).IsRequired();
            entity.Property(u => u.PhoneNumber).HasColumnName("phoneNumber").HasMaxLength(20);
            entity.Property(u => u.DateOfBirth).HasColumnName("dateOfBirth").HasColumnType("date");
            entity.Property(u => u.ProfilePictureUrl).HasColumnName("profilePictureUrl").HasMaxLength(500);
            entity.Property(u => u.CreatedAt).HasColumnName("createdAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_email");

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .HasConstraintName("FK_Users_Role")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_eventType", "[eventType] IN ('Running', 'Walking', 'Cycling')");
                t.HasCheckConstraint("CK_Events_distanceKM", "[distanceKM] > 0");
            });

            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("eventID");
            entity.Property(e => e.OrganiserId).HasColumnName("organiserID");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EventDate).HasColumnName("eventDate").HasColumnType("datetime2");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Province).HasColumnName("province").HasMaxLength(100).IsRequired();
            entity.Property(e => e.EventType).HasColumnName("eventType").HasMaxLength(20).IsRequired();
            entity.Property(e => e.DistanceKm).HasColumnName("distanceKM").HasPrecision(6, 2);
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
            entity.Property(e => e.BannerImageUrl).HasColumnName("bannerImageUrl").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserId)
                .HasConstraintName("FK_Events_Organiser")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", t =>
            {
                t.HasCheckConstraint("CK_Categories_distanceKM", "[distanceKM] > 0");
                t.HasCheckConstraint("CK_Categories_entryFee", "[entryFee] >= 0");
            });

            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.CategoryId).HasColumnName("categoryID");
            entity.Property(c => c.EventId).HasColumnName("eventID");
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(c => c.DistanceKm).HasColumnName("distanceKM").HasPrecision(6, 2);
            entity.Property(c => c.EntryFee).HasColumnName("entryFee").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(c => c.MaxParticipants).HasColumnName("maxParticipants");
            entity.Property(c => c.StartTime).HasColumnName("startTime").HasColumnType("time(0)");
            entity.Property(c => c.CreatedAt).HasColumnName("createdAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            entity.HasIndex(c => new { c.EventId, c.Name }).IsUnique().HasDatabaseName("UQ_Categories_EventName");

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .HasConstraintName("FK_Categories_Event")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", t =>
            {
                t.HasCheckConstraint("CK_Enrolments_status", "[status] IN ('Confirmed', 'Cancelled')");
            });

            entity.HasKey(e => e.EnrolmentId);
            entity.Property(e => e.EnrolmentId).HasColumnName("enrolmentID");
            entity.Property(e => e.EventId).HasColumnName("eventID");
            entity.Property(e => e.CategoryId).HasColumnName("categoryID");
            entity.Property(e => e.ParticipantId).HasColumnName("participantID");
            entity.Property(e => e.RaceNumber).HasColumnName("raceNumber").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("Confirmed");
            entity.Property(e => e.EnrolledAt).HasColumnName("enrolledAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => e.RaceNumber).IsUnique().HasDatabaseName("UQ_Enrolments_raceNumber");
            entity.HasIndex(e => new { e.CategoryId, e.ParticipantId }).IsUnique().HasDatabaseName("UQ_Enrolments_CategoryParticipant");

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .HasConstraintName("FK_Enrolments_Event")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .HasConstraintName("FK_Enrolments_Category")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .HasConstraintName("FK_Enrolments_Participant")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results", t =>
            {
                t.HasCheckConstraint("CK_Results_status", "[status] IN ('Finished', 'DNF', 'DNS')");
                t.HasCheckConstraint("CK_Results_position", "[positionOverall] IS NULL OR [positionOverall] > 0");
            });

            entity.HasKey(r => r.ResultId);
            entity.Property(r => r.ResultId).HasColumnName("resultID");
            entity.Property(r => r.EnrolmentId).HasColumnName("enrolmentID");
            entity.Property(r => r.FinishTime).HasColumnName("finishTime").HasColumnType("time(0)");
            entity.Property(r => r.PositionOverall).HasColumnName("positionOverall");
            entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("Finished");
            entity.Property(r => r.CapturedAt).HasColumnName("capturedAt").HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            entity.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_enrolment");

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .HasConstraintName("FK_Results_Enrolment")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}