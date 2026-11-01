using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InvoicingServicePayments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.AddColumn<Guid>(
            name: "InvoiceId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "InvoiceCounters",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Value = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceCounters", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                QuotationVersion = table.Column<int>(type: "integer", nullable: false),
                QuotationNumber = table.Column<string>(type: "text", nullable: false),
                Number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                IsSimulated = table.Column<bool>(type: "boolean", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
                CustomerName = table.Column<string>(type: "text", nullable: false),
                CustomerEmail = table.Column<string>(type: "text", nullable: false),
                CustomerAddress = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                ServicePeriod = table.Column<string>(type: "text", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                TaxTreatment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                SubtotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TaxMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                TotalMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                LinesJson = table.Column<string>(type: "jsonb", nullable: false),
                SellerName = table.Column<string>(type: "text", nullable: false),
                SellerAddress = table.Column<string>(type: "text", nullable: false),
                RegistrationNumber = table.Column<string>(type: "text", nullable: false),
                TaxInformation = table.Column<string>(type: "text", nullable: false),
                Terms = table.Column<string>(type: "text", nullable: false),
                TermsVersion = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
                table.CheckConstraint("CK_Invoice_Money", "\"SubtotalMinorUnits\" >= 0 AND \"TaxMinorUnits\" >= 0 AND \"TotalMinorUnits\" = \"SubtotalMinorUnits\" + \"TaxMinorUnits\" AND \"TotalMinorUnits\" BETWEEN 1 AND 10000000000");
                table.CheckConstraint("CK_Invoice_Status", "\"Status\" IN ('Draft','Issued','PaymentPending','Paid','Overdue','Cancelled')");
                table.ForeignKey(
                    name: "FK_Invoices_Quotations_QuotationId",
                    column: x => x.QuotationId,
                    principalTable: "Quotations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Invoices_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "InvoiceTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_InvoiceTokens_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PaymentEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                EventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Outcome = table.Column<string>(type: "text", nullable: false),
                ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentEvents_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                ProviderSessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProviderPaymentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AmountMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FailureReasonCode = table.Column<string>(type: "text", nullable: true),
                ReconcileAttempts = table.Column<int>(type: "integer", nullable: false),
                NextReconcileAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DiscrepancyCode = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.CheckConstraint("CK_Payment_Amount", "\"AmountMinorUnits\" BETWEEN 1 AND 10000000000");
                table.ForeignKey(
                    name: "FK_Payments_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResearchEligibility",
            columns: table => new
            {
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                IsSimulated = table.Column<bool>(type: "boolean", nullable: false),
                VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResearchEligibility", x => x.RequestId);
                table.ForeignKey(
                    name: "FK_ResearchEligibility_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResearchEligibility_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SandboxPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<string>(type: "text", nullable: false),
                AccessHash = table.Column<string>(type: "text", nullable: false),
                ProtectedAccess = table.Column<string>(type: "text", nullable: false),
                ProtectedReturnUrl = table.Column<string>(type: "text", nullable: false),
                AmountMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                Currency = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SandboxPayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_SandboxPayments_Invoices_InvoiceId",
                    column: x => x.InvoiceId,
                    principalTable: "Invoices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "InvoiceCounters",
            columns: new[] { "Id", "Value" },
            values: new object[] { 1, 0L });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_InvoiceId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "InvoiceId", "Kind" },
            unique: true,
            filter: "\"InvoiceId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL AND \"InvoiceId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_CreatedAt",
            table: "Invoices",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_Number",
            table: "Invoices",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_QuotationId",
            table: "Invoices",
            column: "QuotationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_RequestId",
            table: "Invoices",
            column: "RequestId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_Status_DueAt",
            table: "Invoices",
            columns: new[] { "Status", "DueAt" });

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceTokens_ExpiresAt",
            table: "InvoiceTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceTokens_Hash",
            table: "InvoiceTokens",
            column: "Hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InvoiceTokens_InvoiceId",
            table: "InvoiceTokens",
            column: "InvoiceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentEvents_InvoiceId",
            table: "PaymentEvents",
            column: "InvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentEvents_Provider_EventId",
            table: "PaymentEvents",
            columns: new[] { "Provider", "EventId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_InvoiceId",
            table: "Payments",
            column: "InvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_Provider_ProviderPaymentId",
            table: "Payments",
            columns: new[] { "Provider", "ProviderPaymentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_Provider_ProviderSessionId",
            table: "Payments",
            columns: new[] { "Provider", "ProviderSessionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_Status_NextReconcileAt",
            table: "Payments",
            columns: new[] { "Status", "NextReconcileAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ResearchEligibility_InvoiceId",
            table: "ResearchEligibility",
            column: "InvoiceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SandboxPayments_AccessHash",
            table: "SandboxPayments",
            column: "AccessHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SandboxPayments_InvoiceId",
            table: "SandboxPayments",
            column: "InvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_SandboxPayments_SessionId",
            table: "SandboxPayments",
            column: "SessionId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_Invoices_InvoiceId",
            table: "NotificationOutbox",
            column: "InvoiceId",
            principalTable: "Invoices",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_Invoices_InvoiceId",
            table: "NotificationOutbox");

        migrationBuilder.DropTable(
            name: "InvoiceCounters");

        migrationBuilder.DropTable(
            name: "InvoiceTokens");

        migrationBuilder.DropTable(
            name: "PaymentEvents");

        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "ResearchEligibility");

        migrationBuilder.DropTable(
            name: "SandboxPayments");

        migrationBuilder.DropTable(
            name: "Invoices");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_InvoiceId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "InvoiceId",
            table: "NotificationOutbox");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_RequestId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "RequestId", "Kind" },
            unique: true,
            filter: "\"QuotationId\" IS NULL");
    }
}
