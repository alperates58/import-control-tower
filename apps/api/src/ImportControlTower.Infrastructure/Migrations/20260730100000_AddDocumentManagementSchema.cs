using System;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportControlTower.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260730100000_AddDocumentManagementSchema")]
    public partial class AddDocumentManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShipmentContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.CheckConstraint("chk_documents_exact_one_scope", "(\"ImportCaseId\" IS NOT NULL AND \"ShipmentId\" IS NULL AND \"ShipmentContainerId\" IS NULL) OR (\"ImportCaseId\" IS NULL AND \"ShipmentId\" IS NOT NULL AND \"ShipmentContainerId\" IS NULL) OR (\"ImportCaseId\" IS NULL AND \"ShipmentId\" IS NULL AND \"ShipmentContainerId\" IS NOT NULL)");
                    table.CheckConstraint("chk_documents_status", "\"Status\" IN ('Active', 'Cancelled')");
                    table.CheckConstraint("chk_documents_dates", "\"ExpiryDate\" IS NULL OR \"DocumentDate\" IS NULL OR \"ExpiryDate\" >= \"DocumentDate\"");
                    table.ForeignKey(
                        name: "FK_documents_import_cases_ImportCaseId",
                        column: x => x.ImportCaseId,
                        principalTable: "import_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_shipment_containers_ShipmentContainerId",
                        column: x => x.ShipmentContainerId,
                        principalTable: "shipment_containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.Id);
                    table.CheckConstraint("chk_document_versions_size", "\"FileSizeBytes\" > 0");
                    table.CheckConstraint("chk_document_versions_ver", "\"VersionNumber\" > 0");
                    table.CheckConstraint("chk_document_versions_hash", "length(\"Sha256Hash\") = 64");
                    table.CheckConstraint("chk_document_versions_status", "\"Status\" IN ('Active', 'Replaced', 'Cancelled')");
                    table.CheckConstraint("chk_document_versions_storage_status", "\"StorageStatus\" IN ('Pending', 'Active', 'Failed', 'CleanupRequired')");
                    table.ForeignKey(
                        name: "FK_document_versions_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_versions_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransportMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_requirements", x => x.Id);
                    table.CheckConstraint("chk_document_requirements_scope", "\"ScopeType\" IN ('ImportCase', 'Shipment')");
                    table.CheckConstraint("chk_document_requirements_sort", "\"SortOrder\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_ImportCaseId",
                table: "documents",
                column: "ImportCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_ShipmentId",
                table: "documents",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_ShipmentContainerId",
                table: "documents",
                column: "ShipmentContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_DocumentId_VersionNumber",
                table: "document_versions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_document_versions_one_current",
                table: "document_versions",
                column: "DocumentId",
                unique: true,
                filter: "\"IsCurrent\" = true AND \"Status\" = 'Active' AND \"StorageStatus\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_document_requirements_ScopeType_TransportMode_DocumentType",
                table: "document_requirements",
                columns: new[] { "ScopeType", "TransportMode", "DocumentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "document_versions");
            migrationBuilder.DropTable(name: "documents");
            migrationBuilder.DropTable(name: "document_requirements");
        }
    }
}
