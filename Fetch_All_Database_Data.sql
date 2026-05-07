-- =============================================
-- ALL GPMS DATA TABLES & RAW ARCHITECTURE VIEW
-- =============================================
USE GPMS_Desktop;
GO

-- 1. View Migrations (Backend Schema Tracker)
SELECT * FROM [__EFMigrationsHistory];
GO

-- 2. View All Assigned Roles
SELECT * FROM [Roles];
GO

-- 3. View All Employees registered in the system
SELECT * FROM [Employee];
GO

-- 4. View All Projects 
SELECT * FROM [Project];
GO

-- 5. View All Modules linked to those projects
SELECT * FROM [Module];
GO

-- 6. View All Sub-Tasks
SELECT * FROM [Task];
GO
