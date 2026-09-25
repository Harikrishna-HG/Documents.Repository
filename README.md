# Documents.Repository

This project is built using ASP.NET Core Web API with the Model-View-Controller (MVC) architecture.
It is designed to provide a robust backend solution leveraging Entity Framework Core.

## ASP.NET Core Web API [MODEL VIEW CONTROLLER]


> Document.Repository

## Database: SQL Server Express
### ORM Tool: EF Core, [Dapper (for advanced query execution)] etc.

## EntityFrameworkCore Commands

### Install EntityFramework tool globally
`dotnet tool install dotnet-ef -g`

### For database migrations
`dotnet ef migrations add Initial -o .\Data\Migrations`

### Applying Migrations to the Database
`dotnet ef database update`

# FWU Central E-Library

FWU Central E-Library is a Razor Pages (.NET 9) web application for managing and accessing academic projects, resources, and digital documents at Farwestern University.

## Features

- Project submission, editing, and approval workflow
- Tagging and categorization of projects
- Student and admin roles with authorization
- PDF viewing and downloading
- Advanced search and filtering
- Responsive UI with Bootstrap carousel and cards

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (or compatible database)
- Visual Studio 2022 or later

### Setup

1. Clone the repository:

SQL Query to be SuperAdmin while registering for first time.

-- Step 1: Get the UserId of the user with the specified email
DECLARE @UserId NVARCHAR(450);
SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'hrkshnagtm@gmail.com';

-- Step 2: Get the RoleId of the "SuperAdmin" role
DECLARE @SuperAdminRoleId NVARCHAR(450);
SELECT @SuperAdminRoleId = Id FROM AspNetRoles WHERE Name = 'SuperAdmin';

-- Step 3: Assign the "SuperAdmin" role to the user
IF NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @SuperAdminRoleId
)
BEGIN
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @SuperAdminRoleId);
END

-- Step 4: Get the RoleId of the "Student" role
DECLARE @StudentRoleId NVARCHAR(450);
SELECT @StudentRoleId = Id FROM AspNetRoles WHERE Name = 'Student';

-- Step 5: Remove the "Student" role from the user
IF EXISTS (
    SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @StudentRoleId
)
BEGIN
    DELETE FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @StudentRoleId;
END
