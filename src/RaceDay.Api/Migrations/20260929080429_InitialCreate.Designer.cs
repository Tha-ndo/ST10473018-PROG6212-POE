using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RaceDay.Api.Data;

#nullable disable

namespace RaceDay.Api.Migrations
{
    [DbContext(typeof(RaceDayDbContext))]
    [Migration("20260929080429_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("RaceDay.Api.Models.AppUser", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("UserID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("PhoneNumber")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("UserId");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Users", null, t =>
                        {
                            t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
                        });
                });

            modelBuilder.Entity("RaceDay.Api.Models.Category", b =>
                {
                    b.Property<int>("CategoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("CategoryID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CategoryId"));

                    b.Property<decimal>("DistanceKm")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.Property<decimal>("EntryFee")
                        .ValueGeneratedOnAdd()
                        .HasPrecision(8, 2)
                        .HasColumnType("decimal(8,2)")
                        .HasDefaultValue(0m);

                    b.Property<int>("EventId")
                        .HasColumnType("int")
                        .HasColumnName("EventID");

                    b.Property<int>("MaxParticipants")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(100);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("CategoryId");

                    b.HasIndex("EventId");

                    b.ToTable("Categories", (string)null);
                });

            modelBuilder.Entity("RaceDay.Api.Models.Enrolment", b =>
                {
                    b.Property<int>("EnrolmentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("EnrolmentID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EnrolmentId"));

                    b.Property<string>("BibNumber")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int")
                        .HasColumnName("CategoryID");

                    b.Property<DateTime>("EnrolmentDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<int>("ParticipantId")
                        .HasColumnType("int")
                        .HasColumnName("ParticipantID");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasDefaultValue("Confirmed");

                    b.HasKey("EnrolmentId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("ParticipantId", "CategoryId")
                        .IsUnique()
                        .HasDatabaseName("UQ_Enrolments_Participant_Category");

                    b.ToTable("Enrolments", null, t =>
                        {
                            t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')");
                        });
                });

            modelBuilder.Entity("RaceDay.Api.Models.RaceEvent", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("EventID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EventId"));

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("GETDATE()");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly>("EventDate")
                        .HasColumnType("date");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("OrganiserId")
                        .HasColumnType("int")
                        .HasColumnName("OrganiserID");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int>("VenueId")
                        .HasColumnType("int")
                        .HasColumnName("VenueID");

                    b.HasKey("EventId");

                    b.HasIndex("OrganiserId");

                    b.HasIndex("VenueId");

                    b.ToTable("Events", null, t =>
                        {
                            t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Running', 'Walking', 'Cycling')");
                        });
                });

            modelBuilder.Entity("RaceDay.Api.Models.RaceResult", b =>
                {
                    b.Property<int>("ResultId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("ResultID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ResultId"));

                    b.Property<int>("EnrolmentId")
                        .HasColumnType("int")
                        .HasColumnName("EnrolmentID");

                    b.Property<TimeSpan?>("FinishTime")
                        .HasColumnType("time");

                    b.Property<int?>("Position")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasDefaultValue("Finished");

                    b.HasKey("ResultId");

                    b.HasIndex("EnrolmentId")
                        .IsUnique();

                    b.ToTable("Results", null, t =>
                        {
                            t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DNS')");
                        });
                });

            modelBuilder.Entity("RaceDay.Api.Models.Venue", b =>
                {
                    b.Property<int>("VenueId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("VenueID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("VenueId"));

                    b.Property<string>("AddressLine")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<decimal?>("Latitude")
                        .HasPrecision(9, 6)
                        .HasColumnType("decimal(9,6)");

                    b.Property<decimal?>("Longitude")
                        .HasPrecision(9, 6)
                        .HasColumnType("decimal(9,6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Province")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("VenueId");

                    b.ToTable("Venues", (string)null);
                });

            modelBuilder.Entity("RaceDay.Api.Models.Category", b =>
                {
                    b.HasOne("RaceDay.Api.Models.RaceEvent", "Event")
                        .WithMany("Categories")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Categories_Event");

                    b.Navigation("Event");
                });

            modelBuilder.Entity("RaceDay.Api.Models.Enrolment", b =>
                {
                    b.HasOne("RaceDay.Api.Models.Category", "Category")
                        .WithMany("Enrolments")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_Enrolments_Category");

                    b.HasOne("RaceDay.Api.Models.AppUser", "Participant")
                        .WithMany("Enrolments")
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_Enrolments_Participant");

                    b.Navigation("Category");

                    b.Navigation("Participant");
                });

            modelBuilder.Entity("RaceDay.Api.Models.RaceEvent", b =>
                {
                    b.HasOne("RaceDay.Api.Models.AppUser", "Organiser")
                        .WithMany("OrganisedEvents")
                        .HasForeignKey("OrganiserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_Events_Organiser");

                    b.HasOne("RaceDay.Api.Models.Venue", "Venue")
                        .WithMany("Events")
                        .HasForeignKey("VenueId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_Events_Venue");

                    b.Navigation("Organiser");

                    b.Navigation("Venue");
                });

            modelBuilder.Entity("RaceDay.Api.Models.RaceResult", b =>
                {
                    b.HasOne("RaceDay.Api.Models.Enrolment", "Enrolment")
                        .WithOne("Result")
                        .HasForeignKey("RaceDay.Api.Models.RaceResult", "EnrolmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Results_Enrolment");

                    b.Navigation("Enrolment");
                });

            modelBuilder.Entity("RaceDay.Api.Models.AppUser", b =>
                {
                    b.Navigation("Enrolments");

                    b.Navigation("OrganisedEvents");
                });

            modelBuilder.Entity("RaceDay.Api.Models.Category", b =>
                {
                    b.Navigation("Enrolments");
                });

            modelBuilder.Entity("RaceDay.Api.Models.Enrolment", b =>
                {
                    b.Navigation("Result");
                });

            modelBuilder.Entity("RaceDay.Api.Models.RaceEvent", b =>
                {
                    b.Navigation("Categories");
                });

            modelBuilder.Entity("RaceDay.Api.Models.Venue", b =>
                {
                    b.Navigation("Events");
                });
#pragma warning restore 612, 618
        }
    }
}
