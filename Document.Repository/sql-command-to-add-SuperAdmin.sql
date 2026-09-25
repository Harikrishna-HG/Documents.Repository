-- Promotes an existing account to SuperAdmin.
--
-- This is only needed for the FIRST promotion. Registration only ever grants the
-- "Student" role (Areas/Identity/Pages/Account/Register.cshtml.cs), and RolesController
-- is [Authorize(Roles = "SuperAdmin")], so the initial assignment cannot be done
-- through the UI. Every later change can use /Roles/AssignRole.
--
-- Usage:
--   1. Set @UserEmail below to the address the account was registered with.
--   2. Run from the repository root:
--        sqlcmd -S "(localdb)\mssqllocaldb" -E -d "Document.Repository.Db" -i Document.Repository\sql-command-to-add-SuperAdmin.sql
--
-- The role is baked into the claims principal issued at sign-in, so the user must
-- log out and back in for this to take effect.

SET NOCOUNT ON;

DECLARE @UserEmail NVARCHAR(450) = N'you@example.com';

DECLARE @UserId NVARCHAR(450);
SELECT @UserId = Id FROM AspNetUsers WHERE Email = @UserEmail;

IF @UserId IS NULL
BEGIN
    THROW 50000, 'No user found with that email. Register the account first, then re-run this script.', 1;
END

DECLARE @SuperAdminRoleId NVARCHAR(450);
SELECT @SuperAdminRoleId = Id FROM AspNetRoles WHERE Name = 'SuperAdmin';

IF @SuperAdminRoleId IS NULL
BEGIN
    THROW 50001, 'Role SuperAdmin not found. Start the application once so RoleSeeder runs.', 1;
END

IF EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @SuperAdminRoleId)
BEGIN
    PRINT 'This account is already a SuperAdmin. Nothing to do.';
    RETURN;
END

INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (@UserId, @SuperAdminRoleId);

-- Drop the auto-granted Student role so the account is not treated as a student.
DELETE FROM AspNetUserRoles
WHERE UserId = @UserId
  AND RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Student');

PRINT 'Promotion applied. Log out and back in for the new role to take effect.';

SELECT u.UserName, u.Email, u.EmailConfirmed, r.Name AS Role
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON ur.UserId = u.Id
JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE u.Id = @UserId;
