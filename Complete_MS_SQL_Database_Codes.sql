do thBuild started...
Build succeeded.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Designation] (
    [designation_id] int NOT NULL IDENTITY,
    [designation_name] varchar(100) NOT NULL,
    [designation_description] varchar(200) NULL,
    CONSTRAINT [PK_Designation] PRIMARY KEY ([designation_id])
);
GO

CREATE TABLE [Permission] (
    [perms_id] int NOT NULL IDENTITY,
    [perms_name] varchar(100) NOT NULL,
    [description] varchar(200) NULL,
    CONSTRAINT [PK_Permission] PRIMARY KEY ([perms_id])
);
GO

CREATE TABLE [Project] (
    [project_id] int NOT NULL IDENTITY,
    [project_name] varchar(100) NOT NULL,
    [project_details] varchar(255) NULL,
    [project_status] varchar(50) NULL,
    [project_start_date] date NOT NULL,
    [project_end_date] date NULL,
    CONSTRAINT [PK_Project] PRIMARY KEY ([project_id])
);
GO

CREATE TABLE [Roles] (
    [role_id] int NOT NULL IDENTITY,
    [role_name] varchar(50) NOT NULL,
    [role_description] varchar(150) NULL,
    [parent_role_id] int NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([role_id]),
    CONSTRAINT [FK_Roles_Roles_parent_role_id] FOREIGN KEY ([parent_role_id]) REFERENCES [Roles] ([role_id])
);
GO

CREATE TABLE [Employee] (
    [employee_id] int NOT NULL IDENTITY,
    [employee_name] varchar(100) NOT NULL,
    [email] varchar(100) NOT NULL,
    [username] varchar(50) NOT NULL,
    [epassword] varchar(255) NOT NULL,
    [designation_id] int NOT NULL,
    CONSTRAINT [PK_Employee] PRIMARY KEY ([employee_id]),
    CONSTRAINT [FK_Employee_Designation_designation_id] FOREIGN KEY ([designation_id]) REFERENCES [Designation] ([designation_id])
);
GO

CREATE TABLE [Module] (
    [module_id] int NOT NULL IDENTITY,
    [project_id] int NOT NULL,
    [module_name] varchar(100) NOT NULL,
    [details] varchar(255) NULL,
    [module_status] varchar(50) NULL,
    [module_start_date] date NULL,
    [module_end_date] date NULL,
    CONSTRAINT [PK_Module] PRIMARY KEY ([module_id]),
    CONSTRAINT [FK_Module_Project_project_id] FOREIGN KEY ([project_id]) REFERENCES [Project] ([project_id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RolePermission] (
    [role_perm_id] int NOT NULL IDENTITY,
    [role_id] int NOT NULL,
    [permission_id] int NOT NULL,
    CONSTRAINT [PK_RolePermission] PRIMARY KEY ([role_perm_id]),
    CONSTRAINT [FK_RolePermission_Permission_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [Permission] ([perms_id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermission_Roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [Roles] ([role_id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Task] (
    [task_id] int NOT NULL IDENTITY,
    [module_id] int NOT NULL,
    [task_name] varchar(100) NOT NULL,
    [task_description] varchar(255) NULL,
    [task_status] varchar(50) NULL,
    [task_start_date] date NULL,
    [task_end_date] date NULL,
    CONSTRAINT [PK_Task] PRIMARY KEY ([task_id]),
    CONSTRAINT [FK_Task_Module_module_id] FOREIGN KEY ([module_id]) REFERENCES [Module] ([module_id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Assignment] (
    [assignment_id] int NOT NULL IDENTITY,
    [project_id] int NULL,
    [module_id] int NULL,
    [task_id] int NULL,
    [employee_id] int NOT NULL,
    [assigned_date] date NOT NULL,
    CONSTRAINT [PK_Assignment] PRIMARY KEY ([assignment_id]),
    CONSTRAINT [FK_Assignment_Employee_employee_id] FOREIGN KEY ([employee_id]) REFERENCES [Employee] ([employee_id]),
    CONSTRAINT [FK_Assignment_Module_module_id] FOREIGN KEY ([module_id]) REFERENCES [Module] ([module_id]),
    CONSTRAINT [FK_Assignment_Project_project_id] FOREIGN KEY ([project_id]) REFERENCES [Project] ([project_id]),
    CONSTRAINT [FK_Assignment_Task_task_id] FOREIGN KEY ([task_id]) REFERENCES [Task] ([task_id])
);
GO

CREATE TABLE [Document] (
    [document_id] int NOT NULL IDENTITY,
    [assignment_id] int NULL,
    [document_name] varchar(100) NOT NULL,
    CONSTRAINT [PK_Document] PRIMARY KEY ([document_id]),
    CONSTRAINT [FK_Document_Assignment_assignment_id] FOREIGN KEY ([assignment_id]) REFERENCES [Assignment] ([assignment_id])
);
GO

CREATE INDEX [IX_Assignment_employee_id] ON [Assignment] ([employee_id]);
GO

CREATE INDEX [IX_Assignment_module_id] ON [Assignment] ([module_id]);
GO

CREATE INDEX [IX_Assignment_project_id] ON [Assignment] ([project_id]);
GO

CREATE INDEX [IX_Assignment_task_id] ON [Assignment] ([task_id]);
GO

CREATE UNIQUE INDEX [UQ__Designat__108F431B97E69E20] ON [Designation] ([designation_name]);
GO

CREATE INDEX [IX_Document_assignment_id] ON [Document] ([assignment_id]);
GO

CREATE INDEX [IX_Employee_designation_id] ON [Employee] ([designation_id]);
GO

CREATE UNIQUE INDEX [UQ__Employee__AB6E61640D4E57BA] ON [Employee] ([email]);
GO

CREATE UNIQUE INDEX [UQ__Employee__F3DBC572E1224198] ON [Employee] ([username]);
GO

CREATE INDEX [IX_Module_project_id] ON [Module] ([project_id]);
GO

CREATE UNIQUE INDEX [UQ__Permissi__B8100928EC646E2C] ON [Permission] ([perms_name]);
GO

CREATE INDEX [IX_RolePermission_permission_id] ON [RolePermission] ([permission_id]);
GO

CREATE UNIQUE INDEX [UQ_Role_Permission] ON [RolePermission] ([role_id], [permission_id]);
GO

CREATE INDEX [IX_Roles_parent_role_id] ON [Roles] ([parent_role_id]);
GO

CREATE UNIQUE INDEX [UQ__Roles__783254B1E5E34E85] ON [Roles] ([role_name]);
GO

CREATE INDEX [IX_Task_module_id] ON [Task] ([module_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260429185353_Initial_Mac_Migration', N'8.0.0');
GO

COMMIT;
GO


