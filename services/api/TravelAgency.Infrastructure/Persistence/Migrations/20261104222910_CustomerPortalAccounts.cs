using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CustomerPortalAccounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "EmailIndex",
            table: "AspNetUsers");

        migrationBuilder.AlterColumn<Guid>(
            name: "RequestId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "CustomerActionId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CustomerId",
            table: "NotificationOutbox",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CustomerStatus",
            table: "BookingEvents",
            type: "character varying(24)",
            maxLength: 24,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "CustomerDisabled",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "AspNetUsers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "IsCustomer",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastLoginAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "AspNetUsers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "PreferredLanguage",
            table: "AspNetUsers",
            type: "character varying(2)",
            maxLength: 2,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "CustomerActions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<string>(type: "text", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                Purpose = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SecurityStamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProtectedIdentityToken = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerActions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerActions_AspNetUsers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CustomerActions_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CustomerRequests",
            columns: table => new
            {
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<string>(type: "text", nullable: false),
                LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Method = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerRequests", x => x.RequestId);
                table.ForeignKey(
                    name: "FK_CustomerRequests_AspNetUsers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CustomerRequests_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CustomerSecurityEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerSecurityEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerSecurityEvents_AspNetUsers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_CustomerActionId",
            table: "NotificationOutbox",
            column: "CustomerActionId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_CustomerId",
            table: "NotificationOutbox",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail",
            unique: true,
            filter: "\"NormalizedEmail\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerActions_CustomerId_Purpose_CreatedAt",
            table: "CustomerActions",
            columns: new[] { "CustomerId", "Purpose", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerActions_ExpiresAt",
            table: "CustomerActions",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerActions_Hash",
            table: "CustomerActions",
            column: "Hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CustomerActions_RequestId",
            table: "CustomerActions",
            column: "RequestId");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerRequests_CustomerId_LinkedAt",
            table: "CustomerRequests",
            columns: new[] { "CustomerId", "LinkedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerSecurityEvents_CustomerId_CreatedAt",
            table: "CustomerSecurityEvents",
            columns: new[] { "CustomerId", "CreatedAt" });

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_AspNetUsers_CustomerId",
            table: "NotificationOutbox",
            column: "CustomerId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_CustomerActions_CustomerActionId",
            table: "NotificationOutbox",
            column: "CustomerActionId",
            principalTable: "CustomerActions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.Sql("UPDATE \"BookingEvents\" SET \"CustomerStatus\" = \"SnapshotJson\"->>'Current' WHERE \"Type\" = 'BookingStatusChanged' AND \"SnapshotJson\"->>'Current' IN ('Pending','InProgress','PartiallyConfirmed','Confirmed','Failed','Cancelled')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_AspNetUsers_CustomerId",
            table: "NotificationOutbox");

        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_CustomerActions_CustomerActionId",
            table: "NotificationOutbox");

        migrationBuilder.DropTable(
            name: "CustomerActions");

        migrationBuilder.DropTable(
            name: "CustomerRequests");

        migrationBuilder.DropTable(
            name: "CustomerSecurityEvents");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_CustomerActionId",
            table: "NotificationOutbox");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_CustomerId",
            table: "NotificationOutbox");

        migrationBuilder.DropIndex(
            name: "EmailIndex",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "CustomerActionId",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "CustomerId",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "CustomerStatus",
            table: "BookingEvents");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "CustomerDisabled",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "FirstName",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "IsCustomer",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "LastLoginAt",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "LastName",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PreferredLanguage",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "UpdatedAt",
            table: "AspNetUsers");

        migrationBuilder.AlterColumn<Guid>(
            name: "RequestId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");
    }
}
