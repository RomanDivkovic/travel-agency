using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BookingManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.CreateSequence(
            name: "booking_reference");

        migrationBuilder.AddColumn<Guid>(
            name: "BookingId",
            table: "RequestActivities",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "BookingId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "BookingRevision",
            table: "NotificationOutbox",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PayloadJson",
            table: "NotificationOutbox",
            type: "jsonb",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Bookings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                ProposalVersion = table.Column<int>(type: "integer", nullable: false),
                ApprovedAlternativeId = table.Column<Guid>(type: "uuid", nullable: false),
                Reference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreatedByAdministratorId = table.Column<string>(type: "text", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                Revision = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                AuthorizationJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DepartureDate = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bookings", x => x.Id);
                table.CheckConstraint("CK_Booking_Status", "\"Status\" IN ('Pending','InProgress','PartiallyConfirmed','Confirmed','Failed','Cancelled')");
                table.ForeignKey(
                    name: "FK_Bookings_AspNetUsers_CreatedByAdministratorId",
                    column: x => x.CreatedByAdministratorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Bookings_ProposalAlternative_ApprovedAlternativeId",
                    column: x => x.ApprovedAlternativeId,
                    principalTable: "ProposalAlternative",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Bookings_TravelProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "TravelProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Bookings_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SupplierReservations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                ProposalItemId = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                ApprovedSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                CurrentSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                RequiresReview = table.Column<bool>(type: "boolean", nullable: false),
                ArrangementVersion = table.Column<int>(type: "integer", nullable: false),
                ReapprovedVersion = table.Column<int>(type: "integer", nullable: true),
                PaymentResponsibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                PaymentStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                PaymentArrangement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                SupplierReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ConfirmationEvidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ConfirmationDocumentReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                TicketingStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CancellationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CancellationReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                CancellationChargeMinorUnits = table.Column<long>(type: "bigint", nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupplierReservations", x => x.Id);
                table.CheckConstraint("CK_Reservation_Charge", "\"CancellationChargeMinorUnits\" IS NULL OR \"CancellationChargeMinorUnits\" BETWEEN 0 AND 10000000000");
                table.CheckConstraint("CK_Reservation_Status", "\"Status\" IN ('NotStarted','InProgress','AwaitingConfirmation','Confirmed','Failed','Cancelled')");
                table.ForeignKey(
                    name: "FK_SupplierReservations_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SupplierReservations_ProposalItems_ProposalItemId",
                    column: x => x.ProposalItemId,
                    principalTable: "ProposalItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "BookingEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ActorId = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookingEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_BookingEvents_AspNetUsers_ActorId",
                    column: x => x.ActorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BookingEvents_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BookingEvents_SupplierReservations_ReservationId",
                    column: x => x.ReservationId,
                    principalTable: "SupplierReservations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RequestActivities_BookingId",
            table: "RequestActivities",
            column: "BookingId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_BookingId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "BookingId", "Kind" },
            unique: true,
            filter: "\"BookingId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL AND \"InvoiceId\" IS NULL AND \"ProposalId\" IS NULL AND \"BookingId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_BookingEvents_ActorId",
            table: "BookingEvents",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_BookingEvents_BookingId_CreatedAt_Id",
            table: "BookingEvents",
            columns: new[] { "BookingId", "CreatedAt", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_BookingEvents_ReservationId",
            table: "BookingEvents",
            column: "ReservationId");

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_ApprovedAlternativeId",
            table: "Bookings",
            column: "ApprovedAlternativeId");

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_CreatedByAdministratorId",
            table: "Bookings",
            column: "CreatedByAdministratorId");

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_ProposalId",
            table: "Bookings",
            column: "ProposalId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_Reference",
            table: "Bookings",
            column: "Reference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_RequestId",
            table: "Bookings",
            column: "RequestId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_Status_DepartureDate",
            table: "Bookings",
            columns: new[] { "Status", "DepartureDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_UpdatedAt",
            table: "Bookings",
            column: "UpdatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierReservations_BookingId_ProposalItemId",
            table: "SupplierReservations",
            columns: new[] { "BookingId", "ProposalItemId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SupplierReservations_ProposalItemId",
            table: "SupplierReservations",
            column: "ProposalItemId");

        migrationBuilder.CreateIndex(
            name: "IX_SupplierReservations_Status",
            table: "SupplierReservations",
            column: "Status");

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_Bookings_BookingId",
            table: "NotificationOutbox",
            column: "BookingId",
            principalTable: "Bookings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RequestActivities_Bookings_BookingId",
            table: "RequestActivities",
            column: "BookingId",
            principalTable: "Bookings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_Bookings_BookingId",
            table: "NotificationOutbox");

        migrationBuilder.DropForeignKey(
            name: "FK_RequestActivities_Bookings_BookingId",
            table: "RequestActivities");

        migrationBuilder.DropTable(
            name: "BookingEvents");

        migrationBuilder.DropTable(
            name: "SupplierReservations");

        migrationBuilder.DropTable(
            name: "Bookings");

        migrationBuilder.DropIndex(
            name: "IX_RequestActivities_BookingId",
            table: "RequestActivities");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_BookingId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "BookingId",
            table: "RequestActivities");

        migrationBuilder.DropColumn(
            name: "BookingId",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "BookingRevision",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "PayloadJson",
            table: "NotificationOutbox");

        migrationBuilder.DropSequence(
            name: "booking_reference");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL AND \"InvoiceId\" IS NULL AND \"ProposalId\" IS NULL");
    }
}
