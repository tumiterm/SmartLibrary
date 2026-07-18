using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLibrary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "BookCopies");

            migrationBuilder.AddColumn<string>(
                name: "ClassificationNumber",
                table: "Books",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BookCopies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "BookCopies",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShelfNumber",
                table: "BookCopies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BranchId",
                table: "BookCopies",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Name",
                table: "Branches",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BookCopies_Branches_BranchId",
                table: "BookCopies",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookCopies_Branches_BranchId",
                table: "BookCopies");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_BookCopies_BranchId",
                table: "BookCopies");

            migrationBuilder.DropColumn(
                name: "ClassificationNumber",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BookCopies");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "BookCopies");

            migrationBuilder.DropColumn(
                name: "ShelfNumber",
                table: "BookCopies");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "BookCopies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
