using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class TravelResearchProposals : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.CreateSequence(
            name: "proposal_reference");

        migrationBuilder.AddColumn<Guid>(
            name: "ProposalId",
            table: "RequestActivities",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ProposalId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ResearchWorkspaces",
            columns: table => new
            {
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResearchWorkspaces", x => x.RequestId);
                table.ForeignKey(
                    name: "FK_ResearchWorkspaces_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProposalSeries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CurrentVersion = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProposalSeries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProposalSeries_ResearchWorkspaces_RequestId",
                    column: x => x.RequestId,
                    principalTable: "ResearchWorkspaces",
                    principalColumn: "RequestId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResearchOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResearchOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResearchOptions_ResearchWorkspaces_RequestId",
                    column: x => x.RequestId,
                    principalTable: "ResearchWorkspaces",
                    principalColumn: "RequestId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TravelProposals",
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
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Introduction = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                Summary = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                TermsSnapshot = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                TermsVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AcceptanceText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                TripJson = table.Column<string>(type: "jsonb", nullable: false),
                PaidServiceJson = table.Column<string>(type: "jsonb", nullable: false),
                ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                SelectedAlternativeId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TravelProposals", x => x.Id);
                table.CheckConstraint("CK_Proposal_Status", "\"Status\" IN ('Draft','Sent','Approved','Rejected','Expired','Superseded','Cancelled')");
                table.CheckConstraint("CK_Proposal_Version", "\"Version\" >= 1");
                table.ForeignKey(
                    name: "FK_TravelProposals_AspNetUsers_CreatedByAdministratorId",
                    column: x => x.CreatedByAdministratorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TravelProposals_ProposalSeries_SeriesId",
                    column: x => x.SeriesId,
                    principalTable: "ProposalSeries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProposalAlternative",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Recommended = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProposalAlternative", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProposalAlternative_TravelProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "TravelProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProposalTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProposalTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProposalTokens_TravelProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "TravelProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProposalDecisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                AlternativeId = table.Column<Guid>(type: "uuid", nullable: true),
                Approved = table.Column<bool>(type: "boolean", nullable: false),
                DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                TermsVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Acknowledgement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Feedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProposalDecisions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProposalDecisions_ProposalAlternative_AlternativeId",
                    column: x => x.AlternativeId,
                    principalTable: "ProposalAlternative",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProposalDecisions_TravelProposals_ProposalId",
                    column: x => x.ProposalId,
                    principalTable: "TravelProposals",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProposalItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AlternativeId = table.Column<Guid>(type: "uuid", nullable: false),
                ResearchOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceVersion = table.Column<Guid>(type: "uuid", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                SnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProposalItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProposalItems_ProposalAlternative_AlternativeId",
                    column: x => x.AlternativeId,
                    principalTable: "ProposalAlternative",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProposalItems_ResearchOptions_ResearchOptionId",
                    column: x => x.ResearchOptionId,
                    principalTable: "ResearchOptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RequestActivities_ProposalId",
            table: "RequestActivities",
            column: "ProposalId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_ProposalId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "ProposalId", "Kind" },
            unique: true,
            filter: "\"ProposalId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL AND \"InvoiceId\" IS NULL AND \"ProposalId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_ProposalAlternative_ProposalId_SortOrder",
            table: "ProposalAlternative",
            columns: new[] { "ProposalId", "SortOrder" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProposalDecisions_AlternativeId",
            table: "ProposalDecisions",
            column: "AlternativeId");

        migrationBuilder.CreateIndex(
            name: "IX_ProposalDecisions_ProposalId",
            table: "ProposalDecisions",
            column: "ProposalId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProposalItems_AlternativeId_ResearchOptionId",
            table: "ProposalItems",
            columns: new[] { "AlternativeId", "ResearchOptionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProposalItems_ResearchOptionId",
            table: "ProposalItems",
            column: "ResearchOptionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProposalSeries_Number",
            table: "ProposalSeries",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProposalSeries_RequestId",
            table: "ProposalSeries",
            column: "RequestId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProposalTokens_ExpiresAt",
            table: "ProposalTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_ProposalTokens_Hash",
            table: "ProposalTokens",
            column: "Hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProposalTokens_ProposalId",
            table: "ProposalTokens",
            column: "ProposalId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ResearchOptions_RequestId_Kind",
            table: "ResearchOptions",
            columns: new[] { "RequestId", "Kind" });

        migrationBuilder.CreateIndex(
            name: "IX_Proposal_Current",
            table: "TravelProposals",
            column: "SeriesId",
            unique: true,
            filter: "\"Status\" IN ('Draft','Sent','Approved')");

        migrationBuilder.CreateIndex(
            name: "IX_TravelProposals_CreatedByAdministratorId",
            table: "TravelProposals",
            column: "CreatedByAdministratorId");

        migrationBuilder.CreateIndex(
            name: "IX_TravelProposals_SeriesId_Version",
            table: "TravelProposals",
            columns: new[] { "SeriesId", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TravelProposals_Status_ValidUntil",
            table: "TravelProposals",
            columns: new[] { "Status", "ValidUntil" });

        migrationBuilder.CreateIndex(
            name: "IX_TravelProposals_UpdatedAt",
            table: "TravelProposals",
            column: "UpdatedAt");

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_TravelProposals_ProposalId",
            table: "NotificationOutbox",
            column: "ProposalId",
            principalTable: "TravelProposals",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RequestActivities_TravelProposals_ProposalId",
            table: "RequestActivities",
            column: "ProposalId",
            principalTable: "TravelProposals",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_TravelProposals_ProposalId",
            table: "NotificationOutbox");

        migrationBuilder.DropForeignKey(
            name: "FK_RequestActivities_TravelProposals_ProposalId",
            table: "RequestActivities");

        migrationBuilder.DropTable(
            name: "ProposalDecisions");

        migrationBuilder.DropTable(
            name: "ProposalItems");

        migrationBuilder.DropTable(
            name: "ProposalTokens");

        migrationBuilder.DropTable(
            name: "ProposalAlternative");

        migrationBuilder.DropTable(
            name: "ResearchOptions");

        migrationBuilder.DropTable(
            name: "TravelProposals");

        migrationBuilder.DropTable(
            name: "ProposalSeries");

        migrationBuilder.DropTable(
            name: "ResearchWorkspaces");

        migrationBuilder.DropIndex(
            name: "IX_RequestActivities_ProposalId",
            table: "RequestActivities");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_ProposalId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "ProposalId",
            table: "RequestActivities");

        migrationBuilder.DropColumn(
            name: "ProposalId",
            table: "NotificationOutbox");

        migrationBuilder.DropSequence(
            name: "proposal_reference");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL AND \"InvoiceId\" IS NULL");
    }
}
