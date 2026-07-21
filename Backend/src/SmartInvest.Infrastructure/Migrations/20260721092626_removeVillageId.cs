using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInvest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeVillageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubProjects_Village_VillageId",
                table: "SubProjects");

            migrationBuilder.RenameColumn(
                name: "VillageId",
                table: "SubProjects",
                newName: "MarkazId");

            migrationBuilder.RenameIndex(
                name: "IX_SubProjects_VillageId",
                table: "SubProjects",
                newName: "IX_SubProjects_MarkazId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubProjects_Markaz_MarkazId",
                table: "SubProjects",
                column: "MarkazId",
                principalTable: "Markaz",
                principalColumn: "MarkazId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubProjects_Markaz_MarkazId",
                table: "SubProjects");

            migrationBuilder.RenameColumn(
                name: "MarkazId",
                table: "SubProjects",
                newName: "VillageId");

            migrationBuilder.RenameIndex(
                name: "IX_SubProjects_MarkazId",
                table: "SubProjects",
                newName: "IX_SubProjects_VillageId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubProjects_Village_VillageId",
                table: "SubProjects",
                column: "VillageId",
                principalTable: "Village",
                principalColumn: "VillageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
