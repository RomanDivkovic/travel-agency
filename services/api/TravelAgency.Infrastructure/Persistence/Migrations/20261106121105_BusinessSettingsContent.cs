using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BusinessSettingsContent : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContentAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ActorId = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContentAudits", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContentAudits_AspNetUsers_ActorId",
                    column: x => x.ActorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ManagedContents",
            columns: table => new
            {
                Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DraftJson = table.Column<string>(type: "jsonb", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                PublishedVersion = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ManagedContents", x => x.Key);
                table.ForeignKey(
                    name: "FK_ManagedContents_AspNetUsers_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "WebsiteMedia",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ContentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                AltSv = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                AltEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Decorative = table.Column<bool>(type: "boolean", nullable: false),
                Archived = table.Column<bool>(type: "boolean", nullable: false),
                VersionToken = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WebsiteMedia", x => x.Id);
                table.ForeignKey(
                    name: "FK_WebsiteMedia_AspNetUsers_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ContentRevisions",
            columns: table => new
            {
                Key = table.Column<string>(type: "character varying(100)", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                DataJson = table.Column<string>(type: "jsonb", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PublishedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContentRevisions", x => new { x.Key, x.Version });
                table.ForeignKey(
                    name: "FK_ContentRevisions_AspNetUsers_PublishedBy",
                    column: x => x.PublishedBy,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ContentRevisions_ManagedContents_Key",
                    column: x => x.Key,
                    principalTable: "ManagedContents",
                    principalColumn: "Key",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PublishedMediaReferences",
            columns: table => new
            {
                Key = table.Column<string>(type: "character varying(100)", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                MediaId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PublishedMediaReferences", x => new { x.Key, x.Version, x.MediaId });
                table.ForeignKey(
                    name: "FK_PublishedMediaReferences_ContentRevisions_Key_Version",
                    columns: x => new { x.Key, x.Version },
                    principalTable: "ContentRevisions",
                    principalColumns: new[] { "Key", "Version" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PublishedMediaReferences_WebsiteMedia_MediaId",
                    column: x => x.MediaId,
                    principalTable: "WebsiteMedia",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContentAudits_ActorId",
            table: "ContentAudits",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_ContentAudits_Key_CreatedAt",
            table: "ContentAudits",
            columns: new[] { "Key", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ContentRevisions_PublishedAt",
            table: "ContentRevisions",
            column: "PublishedAt");

        migrationBuilder.CreateIndex(
            name: "IX_ContentRevisions_PublishedBy",
            table: "ContentRevisions",
            column: "PublishedBy");

        migrationBuilder.CreateIndex(
            name: "IX_ManagedContents_UpdatedBy",
            table: "ManagedContents",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_PublishedMediaReferences_MediaId",
            table: "PublishedMediaReferences",
            column: "MediaId");

        migrationBuilder.CreateIndex(
            name: "IX_WebsiteMedia_Archived_CreatedAt",
            table: "WebsiteMedia",
            columns: new[] { "Archived", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_WebsiteMedia_CreatedBy",
            table: "WebsiteMedia",
            column: "CreatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContentAudits");

        migrationBuilder.DropTable(
            name: "PublishedMediaReferences");

        migrationBuilder.DropTable(
            name: "ContentRevisions");

        migrationBuilder.DropTable(
            name: "WebsiteMedia");

        migrationBuilder.DropTable(
            name: "ManagedContents");
    }
}
