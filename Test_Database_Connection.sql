-- GPMS Desktop Database Test Script
-- You can highlight and run these commands to verify the system!

-- 1. Ensure we are connected to the correct MS SQL Database
USE GPMS_Desktop;
GO

-- 2. Verify Entity Framework Migrations History (proves tables were generated)
SELECT * FROM [__EFMigrationsHistory];
GO

-- 3. Check the structure of your Employees Table
SELECT * FROM [Employee];
GO

-- 4. Check Roles
SELECT * FROM [Roles];
GO
