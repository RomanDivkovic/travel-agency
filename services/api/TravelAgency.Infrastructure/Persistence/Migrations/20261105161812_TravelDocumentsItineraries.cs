using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class TravelDocumentsItineraries : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Kind",
            table: "NotificationOutbox",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32);

        migrationBuilder.CreateTable(
            name: "DocumentBlobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                Key = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                DeleteAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PurgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentBlobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_DocumentBlobs_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Itineraries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                PublishedVersion = table.Column<int>(type: "integer", nullable: true),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Itineraries", x => x.Id);
                table.ForeignKey(
                    name: "FK_Itineraries_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TravelDocuments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                PublishedVersion = table.Column<int>(type: "integer", nullable: true),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TravelDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_TravelDocuments_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TravelDocuments_SupplierReservations_ReservationId",
                    column: x => x.ReservationId,
                    principalTable: "SupplierReservations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ItineraryVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                BookingRevision = table.Column<int>(type: "integer", nullable: false),
                TripJson = table.Column<string>(type: "jsonb", nullable: false),
                EntriesJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedById = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ItineraryVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ItineraryVersions_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ItineraryVersions_Itineraries_SeriesId",
                    column: x => x.SeriesId,
                    principalTable: "Itineraries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DocumentVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                BlobId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Scan = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UploadedById = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_DocumentVersions_AspNetUsers_UploadedById",
                    column: x => x.UploadedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DocumentVersions_DocumentBlobs_BlobId",
                    column: x => x.BlobId,
                    principalTable: "DocumentBlobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DocumentVersions_TravelDocuments_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "TravelDocuments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentBlobs_BookingId",
            table: "DocumentBlobs",
            column: "BookingId");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentBlobs_DeleteAfter",
            table: "DocumentBlobs",
            column: "DeleteAfter");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentBlobs_Key",
            table: "DocumentBlobs",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DocumentVersions_BlobId",
            table: "DocumentVersions",
            column: "BlobId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DocumentVersions_DocumentId_Version",
            table: "DocumentVersions",
            columns: new[] { "DocumentId", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DocumentVersions_Status_UpdatedAt",
            table: "DocumentVersions",
            columns: new[] { "Status", "UpdatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentVersions_UploadedById",
            table: "DocumentVersions",
            column: "UploadedById");

        migrationBuilder.CreateIndex(
            name: "IX_Itineraries_BookingId",
            table: "Itineraries",
            column: "BookingId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ItineraryVersions_CreatedById",
            table: "ItineraryVersions",
            column: "CreatedById");

        migrationBuilder.CreateIndex(
            name: "IX_ItineraryVersions_SeriesId_Version",
            table: "ItineraryVersions",
            columns: new[] { "SeriesId", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TravelDocuments_BookingId_CreatedAt_Id",
            table: "TravelDocuments",
            columns: new[] { "BookingId", "CreatedAt", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_TravelDocuments_ReservationId",
            table: "TravelDocuments",
            column: "ReservationId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DocumentVersions");

        migrationBuilder.DropTable(
            name: "ItineraryVersions");

        migrationBuilder.DropTable(
            name: "DocumentBlobs");

        migrationBuilder.DropTable(
            name: "TravelDocuments");

        migrationBuilder.DropTable(
            name: "Itineraries");

        migrationBuilder.AlterColumn<string>(
            name: "Kind",
            table: "NotificationOutbox",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);
    }
}
