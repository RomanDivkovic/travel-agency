using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CustomerMessaging : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ConversationId",
            table: "NotificationOutbox",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Conversations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<string>(type: "text", nullable: false),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                CreationHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                Subject = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Priority = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastSequence = table.Column<long>(type: "bigint", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerNotifiedSequence = table.Column<long>(type: "bigint", nullable: false),
                AgentNotifiedSequence = table.Column<long>(type: "bigint", nullable: false),
                CustomerNotifyAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AgentNotifyAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Conversations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Conversations_AspNetUsers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Conversations_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Conversations_TravelRequests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "TravelRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ConversationEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<string>(type: "text", nullable: true),
                Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Previous = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                Current = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                MessageSequence = table.Column<long>(type: "bigint", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConversationEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConversationEvents_AspNetUsers_ActorId",
                    column: x => x.ActorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ConversationEvents_Conversations_ConversationId",
                    column: x => x.ConversationId,
                    principalTable: "Conversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ConversationMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                Sequence = table.Column<long>(type: "bigint", nullable: false),
                SenderId = table.Column<string>(type: "text", nullable: true),
                SenderRole = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                Body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConversationMessages_AspNetUsers_SenderId",
                    column: x => x.SenderId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ConversationMessages_Conversations_ConversationId",
                    column: x => x.ConversationId,
                    principalTable: "Conversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ConversationReads",
            columns: table => new
            {
                ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                ParticipantId = table.Column<string>(type: "text", nullable: false),
                Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                LastReadSequence = table.Column<long>(type: "bigint", nullable: false),
                LastReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                EmailNotifications = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConversationReads", x => new { x.ConversationId, x.ParticipantId });
                table.ForeignKey(
                    name: "FK_ConversationReads_AspNetUsers_ParticipantId",
                    column: x => x.ParticipantId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ConversationReads_Conversations_ConversationId",
                    column: x => x.ConversationId,
                    principalTable: "Conversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationOutbox_ConversationId_Kind",
            table: "NotificationOutbox",
            columns: new[] { "ConversationId", "Kind" },
            unique: true,
            filter: "\"ConversationId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_ConversationEvents_ActorId",
            table: "ConversationEvents",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_ConversationEvents_ConversationId_CreatedAt_Id",
            table: "ConversationEvents",
            columns: new[] { "ConversationId", "CreatedAt", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_ConversationMessages_ConversationId_SenderId_ClientId",
            table: "ConversationMessages",
            columns: new[] { "ConversationId", "SenderId", "ClientId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ConversationMessages_ConversationId_SenderRole_Sequence",
            table: "ConversationMessages",
            columns: new[] { "ConversationId", "SenderRole", "Sequence" });

        migrationBuilder.CreateIndex(
            name: "IX_ConversationMessages_ConversationId_Sequence",
            table: "ConversationMessages",
            columns: new[] { "ConversationId", "Sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ConversationMessages_SenderId",
            table: "ConversationMessages",
            column: "SenderId");

        migrationBuilder.CreateIndex(
            name: "IX_ConversationReads_ParticipantId",
            table: "ConversationReads",
            column: "ParticipantId");

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_BookingId",
            table: "Conversations",
            column: "BookingId");

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_ClosedAt",
            table: "Conversations",
            column: "ClosedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_CustomerId_ClientId",
            table: "Conversations",
            columns: new[] { "CustomerId", "ClientId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_CustomerId_LastMessageAt_Id",
            table: "Conversations",
            columns: new[] { "CustomerId", "LastMessageAt", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_Priority_LastMessageAt",
            table: "Conversations",
            columns: new[] { "Priority", "LastMessageAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_RequestId",
            table: "Conversations",
            column: "RequestId");

        migrationBuilder.CreateIndex(
            name: "IX_Conversations_Status_LastMessageAt_Id",
            table: "Conversations",
            columns: new[] { "Status", "LastMessageAt", "Id" });

        migrationBuilder.AddForeignKey(
            name: "FK_NotificationOutbox_Conversations_ConversationId",
            table: "NotificationOutbox",
            column: "ConversationId",
            principalTable: "Conversations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NotificationOutbox_Conversations_ConversationId",
            table: "NotificationOutbox");

        migrationBuilder.DropTable(
            name: "ConversationEvents");

        migrationBuilder.DropTable(
            name: "ConversationMessages");

        migrationBuilder.DropTable(
            name: "ConversationReads");

        migrationBuilder.DropTable(
            name: "Conversations");

        migrationBuilder.DropIndex(
            name: "IX_NotificationOutbox_ConversationId_Kind",
            table: "NotificationOutbox");

        migrationBuilder.DropColumn(
            name: "ConversationId",
            table: "NotificationOutbox");
    }
}
