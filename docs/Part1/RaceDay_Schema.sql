/* ============================================================
   RaceDay Database Schema
   Part 1 - System Planning and Database
   Run in SQL Server Management Studio (SSMS) on a clean instance.
   ============================================================ */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop tables if they already exist, in FK-safe order, so the
   script can be re-run cleanly during testing. */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Venues', 'U') IS NOT NULL DROP TABLE dbo.Venues;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* ============================================================
   TABLE: Users
   Holds both Organisers and Participants; Role distinguishes them.
   ============================================================ */
CREATE TABLE dbo.Users (
    UserID          INT             IDENTITY(1,1)   PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber     NVARCHAR(20)    NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* ============================================================
   TABLE: Venues
   Where an event takes place; supports route/location information.
   ============================================================ */
CREATE TABLE dbo.Venues (
    VenueID         INT             IDENTITY(1,1)   PRIMARY KEY,
    Name            NVARCHAR(100)   NOT NULL,
    AddressLine     NVARCHAR(200)   NULL,
    City            NVARCHAR(100)   NOT NULL,
    Province        NVARCHAR(100)   NULL,
    Latitude        DECIMAL(9,6)    NULL,
    Longitude       DECIMAL(9,6)    NULL
);
GO

/* ============================================================
   TABLE: Events
   Created by an Organiser at a Venue.
   ============================================================ */
CREATE TABLE dbo.Events (
    EventID         INT             IDENTITY(1,1)   PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    VenueID         INT             NOT NULL,
    Title           NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(MAX)   NULL,
    EventDate       DATE            NOT NULL,
    EventType       NVARCHAR(20)    NOT NULL
                        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Running', 'Walking', 'Cycling')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Events_Venue     FOREIGN KEY (VenueID)     REFERENCES dbo.Venues(VenueID)
);
GO

/* ============================================================
   TABLE: Categories
   Distance/price bands within an Event (e.g. 5km, 10km, Half Marathon).
   ============================================================ */
CREATE TABLE dbo.Categories (
    CategoryID      INT             IDENTITY(1,1)   PRIMARY KEY,
    EventID         INT             NOT NULL,
    Name            NVARCHAR(50)    NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    MaxParticipants INT             NOT NULL DEFAULT 100,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID) ON DELETE CASCADE
);
GO

/* ============================================================
   TABLE: Enrolments
   Links a Participant to a Category they have entered.
   ============================================================ */
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT             IDENTITY(1,1)   PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    BibNumber       NVARCHAR(10)    NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Confirmed'
                        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Category    FOREIGN KEY (CategoryID)    REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantID, CategoryID)
);
GO

/* ============================================================
   TABLE: Results
   One result per Enrolment (1:1).
   ============================================================ */
CREATE TABLE dbo.Results (
    ResultID        INT             IDENTITY(1,1)   PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Finished'
                        CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DNS')),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID) ON DELETE CASCADE
);
GO

/* ============================================================
   SEED DATA
   2 Organisers, 2 Participants, 3 Events (each with categories),
   and sample enrolments/results.
   ============================================================ */

-- Users: 2 Organisers, 2 Participants
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Thandiwe Mokoena',   'thandiwe@comrades.org.za',    'HASH_PLACEHOLDER_1', 'Organiser',   '0821234567'),
('Pieter van der Merwe','pieter@capetowncycle.co.za',  'HASH_PLACEHOLDER_2', 'Organiser',   '0839876543'),
('Sipho Nkosi',        'sipho.nkosi@example.com',     'HASH_PLACEHOLDER_3', 'Participant', '0731112222'),
('Amanda Botha',       'amanda.botha@example.com',    'HASH_PLACEHOLDER_4', 'Participant', '0725556666');
GO

-- Venues
INSERT INTO dbo.Venues (Name, AddressLine, City, Province, Latitude, Longitude) VALUES
('Comrades Marathon Route', 'Pietermaritzburg CBD', 'Pietermaritzburg', 'KwaZulu-Natal', -29.600200, 30.379200),
('V&A Waterfront',          'Dock Road',            'Cape Town',        'Western Cape',  -33.903100, 18.420100),
('Soweto Athletics Stadium','Chris Hani Road',       'Soweto',           'Gauteng',       -26.267900, 27.858500);
GO

-- Events: 3 events, one per Organiser/Venue combination
INSERT INTO dbo.Events (OrganiserID, VenueID, Title, Description, EventDate, EventType) VALUES
(1, 1, 'Comrades Marathon 2027',     'Iconic ultramarathon between Pietermaritzburg and Durban.', '2027-06-13', 'Running'),
(2, 2, 'Cape Town Cycle Tour 2027',  'Scenic cycling tour around the Cape Peninsula.',             '2027-03-08', 'Cycling'),
(1, 3, 'Soweto Marathon 2027',       'Community road running event through Soweto.',               '2027-11-07', 'Running');
GO

-- Categories: at least one per event
INSERT INTO dbo.Categories (EventID, Name, DistanceKm, EntryFee, MaxParticipants) VALUES
(1, 'Down Run (Full)', 90.00, 950.00, 20000),
(1, 'Novice 10km',     10.00, 250.00, 2000),
(2, '109km Race',      109.00, 550.00, 35000),
(2, '35km Mini Cycle', 35.00, 350.00, 5000),
(3, 'Half Marathon',   21.10, 300.00, 8000),
(3, '5km Fun Walk',    5.00,  100.00, 3000);
GO

-- Enrolments: participants entering categories
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, BibNumber, Status) VALUES
(3, 1, 'CM-1042', 'Confirmed'),
(4, 3, 'CT-5871', 'Confirmed'),
(3, 5, 'SW-0231', 'Confirmed'),
(4, 6, 'SW-0980', 'Confirmed');
GO

-- Results: captured for two of the enrolments so far
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '08:45:12', 4521, 'Finished'),
(2, '04:12:03', 812,  'Finished');
GO

PRINT 'RaceDay schema created and seeded successfully.';
