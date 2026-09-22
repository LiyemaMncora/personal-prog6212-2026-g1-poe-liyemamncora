IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

DROP TABLE IF EXISTS Results;
DROP TABLE IF EXISTS Enrolments;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Roles;
GO

CREATE TABLE Roles (
    roleID      INT IDENTITY(1,1) PRIMARY KEY,
    roleName    NVARCHAR(20) NOT NULL,

    CONSTRAINT UQ_Roles_roleName UNIQUE (roleName),
    CONSTRAINT CK_Roles_roleName CHECK (roleName IN ('Organiser', 'Participant'))
);
GO

CREATE TABLE Users (
    userID          INT IDENTITY(1,1) PRIMARY KEY,
    roleID          INT NOT NULL,
    firstName       NVARCHAR(100) NOT NULL,
    lastName        NVARCHAR(100) NOT NULL,
    email           NVARCHAR(150) NOT NULL,
    passwordHash    NVARCHAR(255) NOT NULL,
    phoneNumber     NVARCHAR(20) NULL,
    dateOfBirth     DATE NULL,
    createdAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT UQ_Users_email UNIQUE (email),
    CONSTRAINT FK_Users_Role FOREIGN KEY (roleID) REFERENCES Roles(roleID)
);
GO

CREATE TABLE Events (
    eventID         INT IDENTITY(1,1) PRIMARY KEY,
    organiserID     INT NOT NULL,
    name            NVARCHAR(150) NOT NULL,
    description     NVARCHAR(MAX) NULL,
    eventDate       DATETIME2 NOT NULL,
    location        NVARCHAR(150) NOT NULL,
    province        NVARCHAR(100) NOT NULL,
    eventType       NVARCHAR(20) NOT NULL,
    latitude        DECIMAL(9,6) NULL,
    longitude       DECIMAL(9,6) NULL,
    createdAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Events_Organiser FOREIGN KEY (organiserID) REFERENCES Users(userID),
    CONSTRAINT CK_Events_eventType CHECK (eventType IN ('Running', 'Walking', 'Cycling'))
);
GO

CREATE TABLE Categories (
    categoryID      INT IDENTITY(1,1) PRIMARY KEY,
    eventID         INT NOT NULL,
    name            NVARCHAR(100) NOT NULL,
    distanceKM      DECIMAL(6,2) NOT NULL,
    entryFee        DECIMAL(10,2) NOT NULL DEFAULT 0,
    maxParticipants INT NULL,
    startTime       TIME(0) NULL,
    createdAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Categories_Event FOREIGN KEY (eventID) REFERENCES Events(eventID) ON DELETE CASCADE,
    CONSTRAINT UQ_Categories_EventName UNIQUE (eventID, name),
    CONSTRAINT CK_Categories_distanceKM CHECK (distanceKM > 0),
    CONSTRAINT CK_Categories_entryFee CHECK (entryFee >= 0)
);
GO

CREATE TABLE Enrolments (
    enrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    categoryID      INT NOT NULL,
    participantID   INT NOT NULL,
    raceNumber      NVARCHAR(10) NOT NULL,
    status          NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    enrolledAt      DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (categoryID) REFERENCES Categories(categoryID) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (participantID) REFERENCES Users(userID),
    CONSTRAINT UQ_Enrolments_raceNumber UNIQUE (raceNumber),
    CONSTRAINT UQ_Enrolments_CategoryParticipant UNIQUE (categoryID, participantID),
    CONSTRAINT CK_Enrolments_status CHECK (status IN ('Confirmed', 'Cancelled'))
);
GO

CREATE TABLE Results (
    resultID        INT IDENTITY(1,1) PRIMARY KEY,
    enrolmentID     INT NOT NULL,
    finishTime      TIME(0) NULL,
    positionOverall INT NULL,
    status          NVARCHAR(20) NOT NULL DEFAULT 'Finished',
    capturedAt      DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (enrolmentID) REFERENCES Enrolments(enrolmentID) ON DELETE CASCADE,
    CONSTRAINT UQ_Results_enrolment UNIQUE (enrolmentID),
    CONSTRAINT CK_Results_status CHECK (status IN ('Finished', 'DNF', 'DNS')),
    CONSTRAINT CK_Results_position CHECK (positionOverall IS NULL OR positionOverall > 0)
);
GO

INSERT INTO Roles (roleName) VALUES
('Organiser'),
('Participant');
GO

INSERT INTO Users (roleID, firstName, lastName, email, passwordHash, phoneNumber, dateOfBirth) VALUES
(1, N'Thabo',   N'Nkosi',   N'thabo.nkosi@raceday.co.za',    N'HASH_Organiser01',   N'0821234567', '1985-03-14'),
(1, N'Naledi',  N'Mokoena', N'naledi.mokoena@raceday.co.za', N'HASH_Organiser02',   N'0839876543', '1990-07-22'),
(2, N'Sipho',   N'Khumalo', N'sipho.khumalo@gmail.com',      N'HASH_Participant01', N'0712223333', '1996-11-02'),
(2, N'Chantal', N'Pillay',  N'chantal.pillay@gmail.com',     N'HASH_Participant02', N'0734445555', '1993-05-30'),
(2, N'Lerato',  N'Dlamini', N'lerato.dlamini@gmail.com',     N'HASH_Participant03', N'0766667777', '2000-01-18');
GO

INSERT INTO Events (organiserID, name, description, eventDate, location, province, eventType, latitude, longitude) VALUES
(1, N'Durban Beachfront 10km Challenge',
    N'A flat and fast race along the Golden Mile promenade. The race starts at uShaka Marine World.',
    '2026-08-16 06:00:00', N'Durban', N'KwaZulu-Natal', N'Running', -29.867300, 31.045400),

(1, N'Pietermaritzburg Hill Half Marathon',
    N'A hard half marathon through the hills of Pietermaritzburg. Water tables every 3km.',
    '2026-11-08 05:30:00', N'Pietermaritzburg', N'KwaZulu-Natal', N'Running', -29.601100, 30.379400),

(2, N'Cape Winelands Cycle Classic',
    N'A cycling event through the wine farms of Paarl. Helmets are compulsory for all riders.',
    '2026-12-06 07:00:00', N'Paarl', N'Western Cape', N'Cycling', -33.731700, 18.962100);
GO

INSERT INTO Categories (eventID, name, distanceKM, entryFee, maxParticipants, startTime) VALUES
(1, N'10km Run',             10.00, 120.00,  800, '06:00'),
(1, N'5km Fun Run and Walk',  5.00,  60.00,  500, '06:30'),
(2, N'21.1km Half Marathon', 21.10, 180.00, 1000, '05:30'),
(2, N'10km Run',             10.00, 100.00,  600, '06:00'),
(3, N'50km Cycle',           50.00, 350.00, 1200, '07:00'),
(3, N'25km Cycle',           25.00, 200.00,  900, '07:30');
GO

INSERT INTO Enrolments (categoryID, participantID, raceNumber, status) VALUES
(1, 3, N'D1001', N'Confirmed'),
(2, 4, N'D1002', N'Confirmed'),
(1, 5, N'D1003', N'Confirmed'),
(3, 3, N'P2001', N'Confirmed'),
(6, 4, N'C3001', N'Confirmed');
GO

INSERT INTO Results (enrolmentID, finishTime, positionOverall, status) VALUES
(1, '00:47:32', 1, N'Finished'),
(2, '00:31:05', 1, N'Finished'),
(3, '00:52:18', 2, N'Finished');
GO

SELECT 
    e.name AS EventName, 
    e.eventDate, 
    e.location, 
    c.name AS CategoryName, 
    c.distanceKM, 
    c.entryFee
FROM Events e
INNER JOIN Categories c ON c.eventID = e.eventID
ORDER BY e.eventDate, c.distanceKM;

SELECT 
    u.firstName + ' ' + u.lastName AS Participant, 
    e.name AS EventName,
    c.name AS CategoryName, 
    en.raceNumber, 
    r.finishTime, 
    r.positionOverall
FROM Enrolments en
INNER JOIN Users u ON u.userID = en.participantID
INNER JOIN Categories c ON c.categoryID = en.categoryID
INNER JOIN Events e ON e.eventID = c.eventID
LEFT JOIN Results r ON r.enrolmentID = en.enrolmentID
ORDER BY e.eventDate, c.name, r.positionOverall;
GO
