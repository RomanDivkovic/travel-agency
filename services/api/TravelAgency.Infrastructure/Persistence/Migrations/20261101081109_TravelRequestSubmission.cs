using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class TravelRequestSubmission : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence(
            name: "travel_request_reference");

        migrationBuilder.CreateTable(
            name: "TravelRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReferenceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Service = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                Contact_Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Contact_Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                Contact_Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                Contact_PreferredMethod = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Plan_Origin = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                Plan_Destination = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                Plan_FlexibleDestination = table.Column<bool>(type: "boolean", nullable: false),
                Plan_AdditionalStops = table.Column<string[]>(type: "text[]", nullable: false),
                Plan_DateMode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Plan_Departure = table.Column<DateOnly>(type: "date", nullable: true),
                Plan_Return = table.Column<DateOnly>(type: "date", nullable: true),
                Plan_Earliest = table.Column<DateOnly>(type: "date", nullable: true),
                Plan_Latest = table.Column<DateOnly>(type: "date", nullable: true),
                Plan_DurationDays = table.Column<int>(type: "integer", nullable: true),
                Plan_Adults = table.Column<int>(type: "integer", nullable: false),
                Plan_ChildrenAges = table.Column<int[]>(type: "integer[]", nullable: false),
                Plan_Rooms = table.Column<int>(type: "integer", nullable: true),
                Plan_BudgetAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                Plan_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Plan_BudgetScope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Plan_PreferencesJson = table.Column<string>(type: "jsonb", nullable: false),
                Plan_Activities = table.Column<bool>(type: "boolean", nullable: false),
                Plan_SpecialRequests = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TravelRequests", x => x.Id);
                table.CheckConstraint("CK_Request_Budget", "\"Plan_BudgetAmount\" > 0 AND \"Plan_BudgetAmount\" <= 100000000");
                table.CheckConstraint("CK_Request_Status", "\"Status\" = 'Submitted'");
                table.CheckConstraint("CK_Request_Travelers", "\"Plan_Adults\" BETWEEN 1 AND 20 AND cardinality(\"Plan_ChildrenAges\") <= 10 AND \"Plan_Adults\" + cardinality(\"Plan_ChildrenAges\") <= 20");
            });

        migrationBuilder.CreateTable(
            name: "IdempotencyRecords",
            columns: table => new
            {
                KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyRecords", x => x.KeyHash);
                table.ForeignKey(
                    name: "FK_IdempotencyRecords_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NotificationOutbox",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeadLetterAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationOutbox", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationOutbox_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyRecords_ExpiresAt",
            table: "IdempotencyRecords",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyRecords_RequestId",
            table: "IdempotencyRecords",
            column: "RequestId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_NextAttemptAt",
            table: "NotificationOutbox",
            column: "NextAttemptAt",
            filter: "\"SentAt\" IS NULL AND \"DeadLetterAt\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TravelRequests_CreatedAt",
            table: "TravelRequests",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_TravelRequests_ReferenceNumber",
            table: "TravelRequests",
            column: "ReferenceNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TravelRequests_Status_CreatedAt",
            table: "TravelRequests",
            columns: new[] { "Status", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IdempotencyRecords");

        migrationBuilder.DropTable(
            name: "NotificationOutbox");

        migrationBuilder.DropTable(
            name: "TravelRequests");

        migrationBuilder.DropSequence(
            name: "travel_request_reference");
    }
}
