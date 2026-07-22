using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInvest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyContractorAssignmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_ContractType_ContractTypeId",
                table: "ProjectAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_Contractor_ContractorId",
                table: "ProjectAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_ExecutiveAgency_ExecutiveAgencyId",
                table: "ProjectAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_SubProjects_SubProjectId",
                table: "ProjectAssignment");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAssignment_ExecutiveAgencyId",
                table: "ProjectAssignment");

            migrationBuilder.DropColumn(
                name: "ExecutiveAgencyId",
                table: "ProjectAssignment");

            migrationBuilder.AddColumn<int>(
                name: "ExecutiveAgencyId",
                table: "SubProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "ProjectAssignment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ContractorId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutiveAgencyId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAssignmentChangeRequests",
                columns: table => new
                {
                    ChangeRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    RequestedContractValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequestedExpectedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAssignmentChangeRequests", x => x.ChangeRequestId);
                    table.ForeignKey(
                        name: "FK_ProjectAssignmentChangeRequests_ProjectAssignment_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "ProjectAssignment",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubProjects_ExecutiveAgencyId",
                table: "SubProjects",
                column: "ExecutiveAgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ContractorId",
                table: "AspNetUsers",
                column: "ContractorId",
                unique: true,
                filter: "[ContractorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ExecutiveAgencyId",
                table: "AspNetUsers",
                column: "ExecutiveAgencyId",
                unique: true,
                filter: "[ExecutiveAgencyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignmentChangeRequests_AssignmentId",
                table: "ProjectAssignmentChangeRequests",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Contractor_ContractorId",
                table: "AspNetUsers",
                column: "ContractorId",
                principalTable: "Contractor",
                principalColumn: "ContractorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ExecutiveAgency_ExecutiveAgencyId",
                table: "AspNetUsers",
                column: "ExecutiveAgencyId",
                principalTable: "ExecutiveAgency",
                principalColumn: "ExecutiveAgencyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_ContractType_ContractTypeId",
                table: "ProjectAssignment",
                column: "ContractTypeId",
                principalTable: "ContractType",
                principalColumn: "ContractTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_Contractor_ContractorId",
                table: "ProjectAssignment",
                column: "ContractorId",
                principalTable: "Contractor",
                principalColumn: "ContractorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_SubProjects_SubProjectId",
                table: "ProjectAssignment",
                column: "SubProjectId",
                principalTable: "SubProjects",
                principalColumn: "SubProjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubProjects_ExecutiveAgency_ExecutiveAgencyId",
                table: "SubProjects",
                column: "ExecutiveAgencyId",
                principalTable: "ExecutiveAgency",
                principalColumn: "ExecutiveAgencyId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Contractor_ContractorId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ExecutiveAgency_ExecutiveAgencyId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_ContractType_ContractTypeId",
                table: "ProjectAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_Contractor_ContractorId",
                table: "ProjectAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAssignment_SubProjects_SubProjectId",
                table: "ProjectAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_SubProjects_ExecutiveAgency_ExecutiveAgencyId",
                table: "SubProjects");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ProjectAssignmentChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_SubProjects_ExecutiveAgencyId",
                table: "SubProjects");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ContractorId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ExecutiveAgencyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExecutiveAgencyId",
                table: "SubProjects");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "ProjectAssignment");

            migrationBuilder.DropColumn(
                name: "ContractorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExecutiveAgencyId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "ExecutiveAgencyId",
                table: "ProjectAssignment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAssignment_ExecutiveAgencyId",
                table: "ProjectAssignment",
                column: "ExecutiveAgencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_ContractType_ContractTypeId",
                table: "ProjectAssignment",
                column: "ContractTypeId",
                principalTable: "ContractType",
                principalColumn: "ContractTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_Contractor_ContractorId",
                table: "ProjectAssignment",
                column: "ContractorId",
                principalTable: "Contractor",
                principalColumn: "ContractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_ExecutiveAgency_ExecutiveAgencyId",
                table: "ProjectAssignment",
                column: "ExecutiveAgencyId",
                principalTable: "ExecutiveAgency",
                principalColumn: "ExecutiveAgencyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAssignment_SubProjects_SubProjectId",
                table: "ProjectAssignment",
                column: "SubProjectId",
                principalTable: "SubProjects",
                principalColumn: "SubProjectId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
