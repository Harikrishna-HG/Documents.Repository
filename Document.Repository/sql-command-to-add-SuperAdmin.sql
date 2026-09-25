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
