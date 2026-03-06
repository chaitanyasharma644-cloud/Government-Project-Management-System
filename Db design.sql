CREATE TABLE Roles (
    role_id INT PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    role_description VARCHAR(150),
    parent_role_id INT NULL,
    
    CONSTRAINT FK_Roles_Parent 
    FOREIGN KEY (parent_role_id) 
    REFERENCES Roles(role_id)
);

CREATE TABLE Designation (
    designation_id INT PRIMARY KEY,
    designation_name VARCHAR(100) NOT NULL UNIQUE,
    designation_description VARCHAR(200)
);

CREATE TABLE Employee (
    employee_id INT PRIMARY KEY IDENTITY(1,1),
    employee_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    username VARCHAR(50) NOT NULL UNIQUE,
    epassword VARCHAR(255) NOT NULL,
    designation_id INT NOT NULL,

    CONSTRAINT FK_Employee_Designation
    FOREIGN KEY (designation_id)
    REFERENCES Designation(designation_id)
);

CREATE TABLE Project (
    project_id INT PRIMARY KEY IDENTITY(1,1),
    project_name VARCHAR(100) NOT NULL,
    project_details VARCHAR(255),
    project_status VARCHAR(50) CHECK (project_status IN ('Ongoing','Completed')),
    project_start_date DATE NOT NULL,
    project_end_date DATE
);

CREATE TABLE Module (
    module_id INT PRIMARY KEY IDENTITY(1,1),
    project_id INT NOT NULL,
    module_name VARCHAR(100) NOT NULL,
    details VARCHAR(255),
    module_status VARCHAR(50) CHECK (module_status IN ('Ongoing','Completed')),
    module_start_date DATE,
    module_end_date DATE,

    CONSTRAINT FK_Module_Project
    FOREIGN KEY (project_id)
    REFERENCES Project(project_id)
    ON DELETE CASCADE
);

CREATE TABLE Task (
    task_id INT PRIMARY KEY IDENTITY(1,1),
    module_id INT NOT NULL,
    task_name VARCHAR(100) NOT NULL,
    task_description VARCHAR(255),
    task_status VARCHAR(50) CHECK (task_status IN ('Ongoing','Completed')),
    task_start_date DATE,
    task_end_date DATE,

    CONSTRAINT FK_Task_Module
    FOREIGN KEY (module_id)
    REFERENCES Module(module_id)
    ON DELETE CASCADE
);

CREATE TABLE Permission (
    perms_id INT PRIMARY KEY,
    perms_name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(200)
);

CREATE TABLE RolePermission (
    role_perm_id INT PRIMARY KEY IDENTITY(1,1),
    role_id INT NOT NULL,
    permission_id INT NOT NULL,

    CONSTRAINT FK_RolePermission_Role
    FOREIGN KEY (role_id)
    REFERENCES Roles(role_id),

    CONSTRAINT FK_RolePermission_Permission
    FOREIGN KEY (permission_id)
    REFERENCES Permission(perms_id),

    CONSTRAINT UQ_Role_Permission 
    UNIQUE (role_id, permission_id)
);

CREATE TABLE Assignment (
    assignment_id INT PRIMARY KEY IDENTITY(1,1),

    project_id INT NULL,
    module_id INT NULL,
    task_id INT NULL,

    employee_id INT NOT NULL,
    assigned_date DATE NOT NULL,

    CONSTRAINT FK_Assignment_Project
    FOREIGN KEY (project_id)
    REFERENCES Project(project_id),

    CONSTRAINT FK_Assignment_Module
    FOREIGN KEY (module_id)
    REFERENCES Module(module_id),

    CONSTRAINT FK_Assignment_Task
    FOREIGN KEY (task_id)
    REFERENCES Task(task_id),

    CONSTRAINT FK_Assignment_Employee
    FOREIGN KEY (employee_id)
    REFERENCES Employee(employee_id)
);

CREATE TABLE Document (
    document_id INT PRIMARY KEY IDENTITY(1,1),
    assignment_id INT,
    document_name VARCHAR(100) NOT NULL,
    FOREIGN KEY (assignment_id) REFERENCES Assignment(assignment_id)
);

