using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Quotations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.CreateSequence(
            name: "quotation_reference");

        migrationBuilder.AddColumn<Guid>(
            name: "QuotationId",
            table: "RequestActivities",
            type: "uuid",
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Kind",
            table: "NotificationOutbox",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(12)",
            oldMaxLength: 12);

        migrationBuilder.AddColumn<string>(
            name: "ProtectedLink",
            table: "NotificationOutbox",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "QuotationId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "QuotationSeries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CurrentVersion = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuotationSeries", x => x.Id);
                table.ForeignKey(
                    name: "FK_QuotationSeries_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Quotations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CustomerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                CreatedByAdministratorId = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                IncludedServices = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ExcludedServices = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                TermsSnapshot = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                TermsVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AcceptanceTextSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                AcceptedTermsVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CustomerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                TaxTreatment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                SubtotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TaxMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeclinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Quotations", x => x.Id);
                table.CheckConstraint("CK_Quotation_Money", "\"SubtotalMinorUnits\" >= 0 AND \"TaxMinorUnits\" >= 0 AND \"TotalMinorUnits\" = \"SubtotalMinorUnits\" + \"TaxMinorUnits\" AND \"TotalMinorUnits\" <= 10000000000");
                table.CheckConstraint("CK_Quotation_Status", "\"Status\" IN ('Draft','Sent','Accepted','Declined','Expired','Superseded','Cancelled')");
                table.CheckConstraint("CK_Quotation_Version", "\"Version\" >= 1");
                table.ForeignKey(
                    name: "FK_Quotations_AspNetUsers_CreatedByAdministratorId",
                    column: x => x.CreatedByAdministratorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Quotations_QuotationSeries_SeriesId",
                    column: x => x.SeriesId,
                    principalTable: "QuotationSeries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "QuotationLineItem",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                UnitPriceMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TaxRateBasisPoints = table.Column<int>(type: "integer", nullable: false),
                SubtotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TaxMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuotationLineItem", x => x.Id);
                table.CheckConstraint("CK_QuotationLine_Money", "\"Quantity\" BETWEEN 1 AND 100 AND \"UnitPriceMinorUnits\" BETWEEN 0 AND 10000000000 AND \"TaxRateBasisPoints\" BETWEEN 0 AND 10000 AND \"SubtotalMinorUnits\" >= 0 AND \"TaxMinorUnits\" >= 0 AND \"TotalMinorUnits\" = \"SubtotalMinorUnits\" + \"TaxMinorUnits\" AND \"TotalMinorUnits\" <= 10000000000");
                table.ForeignKey(
                    name: "FK_QuotationLineItem_Quotations_QuotationId",
                    column: x => x.QuotationId,
                    principalTable: "Quotations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "QuotationTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuotationTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_QuotationTokens_Quotations_QuotationId",
                    column: x => x.QuotationId,
                    principalTable: "Quotations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RequestActivities_QuotationId",
            table: "RequestActivities",
            column: "QuotationId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_QuotationId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "QuotationId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_QuotationLineItem_QuotationId_SortOrder",
            table: "QuotationLineItem",
            columns: new[] { "QuotationId", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_Quotation_ActiveVersion",
            table: "Quotations",
            column: "SeriesId",
            unique: true,
            filter: "\"Status\" IN ('Draft', 'Sent', 'Accepted')");

        migrationBuilder.CreateIndex(
            name: "IX_Quotations_CreatedAt",
            table: "Quotations",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Quotations_CreatedByAdministratorId",
            table: "Quotations",
            column: "CreatedByAdministratorId");

        migrationBuilder.CreateIndex(
            name: "IX_Quotations_SeriesId_Version",
            table: "Quotations",
            columns: new[] { "SeriesId", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Quotations_Status_ValidUntil",
            table: "Quotations",
            columns: new[] { "Status", "ValidUntil" });

        migrationBuilder.CreateIndex(
            name: "IX_Quotations_UpdatedAt",
            table: "Quotations",
            column: "UpdatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_QuotationSeries_Number",
            table: "QuotationSeries",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_QuotationSeries_RequestId",
            table: "QuotationSeries",
            column: "RequestId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_QuotationTokens_ExpiresAt",
            table: "QuotationTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_QuotationTokens_Hash",
            table: "QuotationTokens",
            column: "Hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_QuotationTokens_QuotationId",
            table: "QuotationTokens",
            column: "QuotationId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_Quotations_QuotationId",
            table: "NotificationOutbox",
            column: "QuotationId",
            principalTable: "Quotations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RequestActivities_Quotations_QuotationId",
            table: "RequestActivities",
            column: "QuotationId",
            principalTable: "Quotations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_Quotations_QuotationId",
            table: "NotificationOutbox");

        migrationBuilder.DropForeignKey(
            name: "FK_RequestActivities_Quotations_QuotationId",
            table: "RequestActivities");

        migrationBuilder.DropTable(
            name: "QuotationLineItem");

        migrationBuilder.DropTable(
            name: "QuotationTokens");

        migrationBuilder.DropTable(
            name: "Quotations");

        migrationBuilder.DropTable(
            name: "QuotationSeries");

        migrationBuilder.DropIndex(
            name: "IX_RequestActivities_QuotationId",
            table: "RequestActivities");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_QuotationId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "QuotationId",
            table: "RequestActivities");

        migrationBuilder.DropColumn(
            name: "ProtectedLink",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "QuotationId",
            table: "NotificationOutbox");

        migrationBuilder.DropSequence(
            name: "quotation_reference");

        migrationBuilder.AlterColumn<string>(
            name: "Kind",
            table: "NotificationOutbox",
            type: "character varying(12)",
            maxLength: 12,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true);
    }
}
