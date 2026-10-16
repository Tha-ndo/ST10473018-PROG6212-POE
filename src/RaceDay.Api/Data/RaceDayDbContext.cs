using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("UserID");
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Venue>(e =>
        {
            e.ToTable("Venues");
            e.HasKey(x => x.VenueId);
            e.Property(x => x.VenueId).HasColumnName("VenueID");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.AddressLine).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.Province).HasMaxLength(100);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<RaceEvent>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Running', 'Walking', 'Cycling')"));
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).HasColumnName("EventID");
            e.Property(x => x.OrganiserId).HasColumnName("OrganiserID");
            e.Property(x => x.VenueId).HasColumnName("VenueID");
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description); // NVARCHAR(MAX)
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId).HasConstraintName("FK_Events_Organiser")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venue).WithMany(v => v.Events)
                .HasForeignKey(x => x.VenueId).HasConstraintName("FK_Events_Venue")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryId).HasColumnName("CategoryID");
            e.Property(x => x.EventId).HasColumnName("EventID");
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.EntryFee).HasPrecision(8, 2).HasDefaultValue(0m);
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);

            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).HasConstraintName("FK_Categories_Event")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')"));
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentId).HasColumnName("EnrolmentID");
            e.Property(x => x.ParticipantId).HasColumnName("ParticipantID");
            e.Property(x => x.CategoryId).HasColumnName("CategoryID");
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.BibNumber).HasMaxLength(10);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(EnrolmentStatus.Confirmed);

            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique()
                .HasDatabaseName("UQ_Enrolments_Participant_Category");

            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId).HasConstraintName("FK_Enrolments_Participant")
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).HasConstraintName("FK_Enrolments_Category")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RaceResult>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DNS')"));
            e.HasKey(x => x.ResultId);
            e.Property(x => x.ResultId).HasColumnName("ResultID");
            e.Property(x => x.EnrolmentId).HasColumnName("EnrolmentID");
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(ResultStatus.Finished);

            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<RaceResult>(x => x.EnrolmentId).HasConstraintName("FK_Results_Enrolment")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
