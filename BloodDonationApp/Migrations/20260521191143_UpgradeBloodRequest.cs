using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonationApp.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeBloodRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BloodRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "BloodRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FulfilledAt",
                table: "BloodRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FulfilledBy",
                table: "BloodRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "BloodRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequesterEmail",
                table: "BloodRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterName",
                table: "BloodRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UrgencyLevel",
                table: "BloodRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_FulfilledBy",
                table: "BloodRequests",
                column: "FulfilledBy");

            migrationBuilder.AddForeignKey(
                name: "FK_BloodRequests_Donors_FulfilledBy",
                table: "BloodRequests",
                column: "FulfilledBy",
                principalTable: "Donors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BloodRequests_Donors_FulfilledBy",
                table: "BloodRequests");

            migrationBuilder.DropIndex(
                name: "IX_BloodRequests_FulfilledBy",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "FulfilledAt",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "FulfilledBy",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "RequesterEmail",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "RequesterName",
                table: "BloodRequests");

            migrationBuilder.DropColumn(
                name: "UrgencyLevel",
                table: "BloodRequests");
        }
    }
}
