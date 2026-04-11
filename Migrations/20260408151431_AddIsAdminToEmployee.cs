using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GPMS.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdminToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Designation",
                columns: table => new
                {
                    designation_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    designation_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    designation_description = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designation", x => x.designation_id);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    perms_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    perms_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.perms_id);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    project_details = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    project_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    project_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    project_end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.project_id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    role_description = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    parent_role_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.role_id);
                    table.ForeignKey(
                        name: "FK_Roles_Roles_parent_role_id",
                        column: x => x.parent_role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    employee_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    epassword = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    designation_id = table.Column<int>(type: "int", nullable: true),
                    system_role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.employee_id);
                    table.ForeignKey(
                        name: "FK_Employee_Designation_designation_id",
                        column: x => x.designation_id,
                        principalTable: "Designation",
                        principalColumn: "designation_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Module",
                columns: table => new
                {
                    module_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    module_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    details = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    module_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    module_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    module_end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module", x => x.module_id);
                    table.ForeignKey(
                        name: "FK_Module_Project_project_id",
                        column: x => x.project_id,
                        principalTable: "Project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    role_perm_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.role_perm_id);
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "Permission",
                        principalColumn: "perms_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermission_Roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Task",
                columns: table => new
                {
                    task_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    module_id = table.Column<int>(type: "int", nullable: false),
                    task_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    task_description = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    task_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    task_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    task_end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Task", x => x.task_id);
                    table.ForeignKey(
                        name: "FK_Task_Module_module_id",
                        column: x => x.module_id,
                        principalTable: "Module",
                        principalColumn: "module_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignment",
                columns: table => new
                {
                    assignment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    module_id = table.Column<int>(type: "int", nullable: true),
                    task_id = table.Column<int>(type: "int", nullable: true),
                    employee_id = table.Column<int>(type: "int", nullable: false),
                    assigned_date = table.Column<DateOnly>(type: "date", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignment", x => x.assignment_id);
                    table.ForeignKey(
                        name: "FK_Assignment_Employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "Employee",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignment_Module_module_id",
                        column: x => x.module_id,
                        principalTable: "Module",
                        principalColumn: "module_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignment_Project_project_id",
                        column: x => x.project_id,
                        principalTable: "Project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignment_Roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignment_Task_task_id",
                        column: x => x.task_id,
                        principalTable: "Task",
                        principalColumn: "task_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    document_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignment_id = table.Column<int>(type: "int", nullable: true),
                    document_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Document", x => x.document_id);
                    table.ForeignKey(
                        name: "FK_Document_Assignment_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "Assignment",
                        principalColumn: "assignment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_employee_id_project_id_module_id_task_id",
                table: "Assignment",
                columns: new[] { "employee_id", "project_id", "module_id", "task_id" },
                unique: true,
                filter: "[project_id] IS NOT NULL AND [module_id] IS NOT NULL AND [task_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_module_id",
                table: "Assignment",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_project_id",
                table: "Assignment",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_role_id",
                table: "Assignment",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_task_id",
                table: "Assignment",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Designat__108F431B97E69E20",
                table: "Designation",
                column: "designation_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Document_assignment_id",
                table: "Document",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_designation_id",
                table: "Employee",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Employee__AB6E61640D4E57BA",
                table: "Employee",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Employee__F3DBC572E1224198",
                table: "Employee",
                column: "username",
                unique: true,
                filter: "[username] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Module_project_id",
                table: "Module",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Permissi__B8100928EC646E2C",
                table: "Permission",
                column: "perms_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_permission_id",
                table: "RolePermission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Role_Permission",
                table: "RolePermission",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_parent_role_id",
                table: "Roles",
                column: "parent_role_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Roles__783254B1E5E34E85",
                table: "Roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Task_module_id",
                table: "Task",
                column: "module_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Document");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "Assignment");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Task");

            migrationBuilder.DropTable(
                name: "Designation");

            migrationBuilder.DropTable(
                name: "Module");

            migrationBuilder.DropTable(
                name: "Project");
        }
    }
}
