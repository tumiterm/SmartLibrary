using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLibrary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryAndWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReturnBranchId",
                table: "Loans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "LibrarySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxOverdueItems",
                table: "LibrarySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAtUtc",
                table: "BranchTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BranchTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsReferenceOnly",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Stocktakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpectedCount = table.Column<int>(type: "int", nullable: false),
                    ScannedCount = table.Column<int>(type: "int", nullable: false),
                    MissingCount = table.Column<int>(type: "int", nullable: false),
                    FoundCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocktakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocktakes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeScan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StocktakeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookCopyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WasFound = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeScan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocktakeScan_BookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "BookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeScan_Stocktakes_StocktakeId",
                        column: x => x.StocktakeId,
                        principalTable: "Stocktakes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ReturnBranchId",
                table: "Loans",
                column: "ReturnBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakes_BranchId",
                table: "Stocktakes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeScan_BookCopyId",
                table: "StocktakeScan",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeScan_StocktakeId_BookCopyId",
                table: "StocktakeScan",
                columns: new[] { "StocktakeId", "BookCopyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_Branches_ReturnBranchId",
                table: "Loans",
                column: "ReturnBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_Branches_ReturnBranchId",
                table: "Loans");

            migrationBuilder.DropTable(
                name: "StocktakeScan");

            migrationBuilder.DropTable(
                name: "Stocktakes");

            migrationBuilder.DropIndex(
                name: "IX_Loans_ReturnBranchId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ReturnBranchId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "LibrarySettings");

            migrationBuilder.DropColumn(
                name: "MaxOverdueItems",
                table: "LibrarySettings");

            migrationBuilder.DropColumn(
                name: "DispatchedAtUtc",
                table: "BranchTransfers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BranchTransfers");

            migrationBuilder.DropColumn(
                name: "IsReferenceOnly",
                table: "Books");
        }
    }
}
