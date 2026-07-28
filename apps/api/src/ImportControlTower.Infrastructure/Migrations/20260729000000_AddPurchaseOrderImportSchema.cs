using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportControlTower.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderImportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    FileSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    TotalRowCount = table.Column<int>(type: "integer", nullable: false),
                    ValidRowCount = table.Column<int>(type: "integer", nullable: false),
                    InvalidRowCount = table.Column<int>(type: "integer", nullable: false),
                    WarningRowCount = table.Column<int>(type: "integer", nullable: false),
                    ImportedOrderCount = table.Column<int>(type: "integer", nullable: false),
                    ImportedLineCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ParserVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batches", x => x.Id);
                    table.CheckConstraint("chk_import_batch_status", "\"Status\" IN ('Uploaded','Parsing','MappingRequired','Validating','ValidationFailed','ReadyForConfirmation','Importing','Completed','Failed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_import_batches_users_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_import_batches_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedOrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedSupplierName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "oid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "import_confirmation_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_confirmation_requests", x => x.Id);
                    table.CheckConstraint("chk_confirm_req_status", "\"Status\" IN ('Processing','Completed','Failed')");
                    table.ForeignKey(
                        name: "FK_import_confirmation_requests_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    StockCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedStockCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StockName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SasDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "oid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.Id);
                    table.CheckConstraint("chk_po_line_quantities", "\"RemainingQuantity\" >= 0 AND \"OrderedQuantity\" > 0 AND \"RemainingQuantity\" <= \"OrderedQuantity\"");
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "import_batch_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    RawDataJson = table.Column<string>(type: "jsonb", nullable: false),
                    NormalizedDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    ValidationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorCodesJson = table.Column<string>(type: "jsonb", nullable: true),
                    WarningCodesJson = table.Column<string>(type: "jsonb", nullable: true),
                    MatchedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchedLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImportAction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batch_rows", x => x.Id);
                    table.CheckConstraint("chk_batch_row_action", "\"ImportAction\" IN ('CreateOrder','CreateLine','SkipDuplicate','Conflict','Invalid')");
                    table.CheckConstraint("chk_batch_row_status", "\"ValidationStatus\" IN ('Valid','Warning','Error')");
                    table.ForeignKey(
                        name: "FK_import_batch_rows_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_import_batch_rows_purchase_order_lines_MatchedLineId",
                        column: x => x.MatchedLineId,
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_import_batch_rows_purchase_orders_MatchedOrderId",
                        column: x => x.MatchedOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_import_batch_rows_ImportBatchId_RowNumber",
                table: "import_batch_rows",
                columns: new[] { "ImportBatchId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_import_batch_rows_MatchedLineId",
                table: "import_batch_rows",
                column: "MatchedLineId");

            migrationBuilder.CreateIndex(
                name: "IX_import_batch_rows_MatchedOrderId",
                table: "import_batch_rows",
                column: "MatchedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_import_batch_rows_ValidationStatus",
                table: "import_batch_rows",
                column: "ValidationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_import_batches_FileSha256",
                table: "import_batches",
                column: "FileSha256");

            migrationBuilder.CreateIndex(
                name: "IX_import_batches_Status",
                table: "import_batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_import_batches_UploadedByUserId",
                table: "import_batches",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_confirmation_requests_ImportBatchId_IdempotencyKey",
                table: "import_confirmation_requests",
                columns: new[] { "ImportBatchId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_po_lines_NormalizedStockCode",
                table: "purchase_order_lines",
                column: "NormalizedStockCode");

            migrationBuilder.CreateIndex(
                name: "IX_po_lines_PurchaseOrderId_NormalizedStockCode",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "NormalizedStockCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_NormalizedOrderNumber_NormalizedSupplierName",
                table: "purchase_orders",
                columns: new[] { "NormalizedOrderNumber", "NormalizedSupplierName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrderDate",
                table: "purchase_orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_Status",
                table: "purchase_orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "import_batch_rows");
            migrationBuilder.DropTable(name: "import_confirmation_requests");
            migrationBuilder.DropTable(name: "purchase_order_lines");
            migrationBuilder.DropTable(name: "import_batches");
            migrationBuilder.DropTable(name: "purchase_orders");
        }
    }
}
