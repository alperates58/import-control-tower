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
    [Migration("20260730000000_AddImportCasesAndShipmentsSchema")]
    public partial class AddImportCasesAndShipmentsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_number_counters",
                columns: table => new
                {
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_number_counters", x => new { x.DocumentType, x.Year });
                    table.CheckConstraint("chk_doc_counter_values", "\"Year\" >= 2000 AND \"LastNumber\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "operation_idempotency_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_idempotency_requests", x => x.Id);
                    table.CheckConstraint("chk_idempotency_req_status", "\"Status\" IN ('Processing', 'Completed', 'Failed') AND (\"ResponseStatusCode\" IS NULL OR (\"ResponseStatusCode\" >= 100 AND \"ResponseStatusCode\" <= 599))");
                    table.ForeignKey(
                        name: "FK_operation_idempotency_requests_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedSupplierName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    OriginCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultTransportMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Incoterm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchasingOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationsOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ProductionStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EstimatedProductionCompletionDate = table.Column<DateTime>(type: "date", nullable: true),
                    ReadyForShipmentDate = table.Column<DateTime>(type: "date", nullable: true),
                    LastShipmentSequence = table.Column<int>(type: "integer", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_cases", x => x.Id);
                    table.CheckConstraint("chk_import_case_default_mode", "\"DefaultTransportMode\" IS NULL OR \"DefaultTransportMode\" IN ('Sea', 'Air', 'Road', 'Rail', 'Courier', 'Multimodal')");
                    table.CheckConstraint("chk_import_case_prod_status", "\"ProductionStatus\" IN ('NotStarted', 'Started', 'Delayed', 'Completed', 'ReadyForShipment')");
                    table.CheckConstraint("chk_import_case_seq", "\"LastShipmentSequence\" >= 0");
                    table.CheckConstraint("chk_import_case_status", "\"Status\" IN ('Draft', 'Active', 'Closed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_import_cases_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_cases_users_OperationsOwnerUserId",
                        column: x => x.OperationsOwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_cases_users_PurchasingOwnerUserId",
                        column: x => x.PurchasingOwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_cases_users_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_cases_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_case_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PlannedShipmentDate = table.Column<DateTime>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_case_lines", x => x.Id);
                    table.UniqueConstraint("AK_import_case_lines_Id_ImportCaseId", x => new { x.Id, x.ImportCaseId });
                    table.CheckConstraint("chk_import_case_line_quantities", "\"AllocatedQuantity\" > 0 AND \"ReleasedQuantity\" >= 0 AND \"ReleasedQuantity\" <= \"AllocatedQuantity\"");
                    table.CheckConstraint("chk_import_case_line_status", "\"Status\" IN ('Allocated', 'PartiallyShipped', 'FullyShipped', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_import_case_lines_import_cases_ImportCaseId",
                        column: x => x.ImportCaseId,
                        principalTable: "import_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_case_lines_purchase_order_lines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_case_lines_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_import_case_lines_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentSequence = table.Column<int>(type: "integer", nullable: false),
                    ShipmentNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    TransportMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BookingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OriginLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DestinationLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ForwarderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CarrierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TransportReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VesselName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VoyageNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OriginTimezoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinationTimezoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Etd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Eta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Atd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ata = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedWarehouseArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualWarehouseArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModeSpecificMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.Id);
                    table.UniqueConstraint("AK_shipments_Id_ImportCaseId", x => new { x.Id, x.ImportCaseId });
                    table.CheckConstraint("chk_shipment_mode", "\"TransportMode\" IN ('Sea', 'Air', 'Road', 'Rail', 'Courier', 'Multimodal')");
                    table.CheckConstraint("chk_shipment_sequence", "\"ShipmentSequence\" > 0");
                    table.CheckConstraint("chk_shipment_status", "\"Status\" IN ('Draft', 'BookingPending', 'Booked', 'Loading', 'InTransit', 'Arrived', 'Delivered', 'Cancelled', 'Aborted')");
                    table.ForeignKey(
                        name: "FK_shipments_import_cases_ImportCaseId",
                        column: x => x.ImportCaseId,
                        principalTable: "import_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedContainerNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContainerType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SealNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GrossWeightKg = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    NetWeightKg = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PackageCount = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_containers", x => x.Id);
                    table.CheckConstraint("chk_shipment_container_status", "\"Status\" IN ('Assigned', 'Loaded', 'InTransit', 'Discharged', 'Delivered', 'Returned', 'Cancelled')");
                    table.CheckConstraint("chk_shipment_container_type", "\"ContainerType\" IN ('20GP', '40GP', '40HC', '45HC', 'LCL', 'Reefer', 'OpenTop', 'FlatRack', 'Other')");
                    table.CheckConstraint("chk_shipment_container_weights", "(\"GrossWeightKg\" IS NULL OR \"GrossWeightKg\" > 0) AND (\"NetWeightKg\" IS NULL OR \"NetWeightKg\" >= 0) AND (\"GrossWeightKg\" IS NULL OR \"NetWeightKg\" IS NULL OR \"NetWeightKg\" <= \"GrossWeightKg\") AND (\"PackageCount\" IS NULL OR \"PackageCount\" > 0)");
                    table.ForeignKey(
                        name: "FK_shipment_containers_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_containers_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_containers_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_line_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportCaseLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ShippedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_line_allocations", x => x.Id);
                    table.CheckConstraint("chk_shipment_line_alloc_quantities", "\"AllocatedQuantity\" > 0 AND \"ReleasedQuantity\" >= 0 AND \"ShippedQuantity\" >= 0 AND \"ReceivedQuantity\" >= 0 AND \"ShippedQuantity\" <= (\"AllocatedQuantity\" - \"ReleasedQuantity\") AND \"ReceivedQuantity\" <= \"ShippedQuantity\"");
                    table.CheckConstraint("chk_shipment_line_alloc_status", "\"Status\" IN ('Allocated', 'Shipped', 'Received', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_import_case_lines_ImportCaseLine~",
                        column: x => x.ImportCaseLineId,
                        principalTable: "import_case_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_import_case_lines_ImportCaseLine~1",
                        columns: x => new { x.ImportCaseLineId, x.ImportCaseId },
                        principalTable: "import_case_lines",
                        principalColumns: new[] { "Id", "ImportCaseId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_import_cases_ImportCaseId",
                        column: x => x.ImportCaseId,
                        principalTable: "import_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_shipments_ShipmentId_ImportCaseId",
                        columns: x => new { x.ShipmentId, x.ImportCaseId },
                        principalTable: "shipments",
                        principalColumns: new[] { "Id", "ImportCaseId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_line_allocations_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    MilestoneType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    LocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TimezoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlannedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_milestones", x => x.Id);
                    table.CheckConstraint("chk_milestone_completed_date", "\"Status\" != 'Completed' OR \"ActualAtUtc\" IS NOT NULL");
                    table.CheckConstraint("chk_milestone_seq", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("chk_milestone_source", "\"Source\" IN ('Manual', 'SystemDerived')");
                    table.CheckConstraint("chk_milestone_status", "\"Status\" IN ('Pending', 'InProgress', 'Completed', 'Skipped', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_shipment_milestones_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_milestones_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_milestones_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_status_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_status_histories", x => x.Id);
                    table.CheckConstraint("chk_status_history_entity_ref", "(\"EntityType\" = 'ImportCase' AND \"ImportCaseId\" IS NOT NULL AND \"ShipmentId\" IS NULL) OR (\"EntityType\" = 'Shipment' AND \"ShipmentId\" IS NOT NULL AND \"ImportCaseId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_shipment_status_histories_import_cases_ImportCaseId",
                        column: x => x.ImportCaseId,
                        principalTable: "import_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_status_histories_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_status_histories_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_import_case_lines_CreatedByUserId",
                table: "import_case_lines",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_case_lines_ImportCaseId_PurchaseOrderLineId",
                table: "import_case_lines",
                columns: new[] { "ImportCaseId", "PurchaseOrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_import_case_lines_PurchaseOrderLineId",
                table: "import_case_lines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_import_case_lines_UpdatedByUserId",
                table: "import_case_lines",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_CaseNumber",
                table: "import_cases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_CreatedAtUtc",
                table: "import_cases",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_CreatedByUserId",
                table: "import_cases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_NormalizedSupplierName",
                table: "import_cases",
                column: "NormalizedSupplierName");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_OperationsOwnerUserId",
                table: "import_cases",
                column: "OperationsOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_PurchasingOwnerUserId",
                table: "import_cases",
                column: "PurchasingOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_ResponsibleUserId",
                table: "import_cases",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_Status",
                table: "import_cases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_import_cases_UpdatedAtUtc",
                table: "import_cases",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_operation_idempotency_requests_RequestedByUserId_Operatio~",
                table: "operation_idempotency_requests",
                columns: new[] { "RequestedByUserId", "OperationType", "ScopeKey", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_containers_CreatedByUserId",
                table: "shipment_containers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_containers_NormalizedContainerNumber",
                table: "shipment_containers",
                column: "NormalizedContainerNumber");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_containers_ShipmentId",
                table: "shipment_containers",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_containers_UpdatedByUserId",
                table: "shipment_containers",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_line_allocations_CreatedByUserId",
                table: "shipment_line_allocations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_line_allocations_ImportCaseId",
                table: "shipment_line_allocations",
                column: "ImportCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_line_allocations_ImportCaseLineId_ImportCaseId",
                table: "shipment_line_allocations",
                columns: new[] { "ImportCaseLineId", "ImportCaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_line_allocations_ShipmentId_ImportCaseId",
                table: "shipment_line_allocations",
                columns: new[] { "ShipmentId", "ImportCaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_line_allocations_ShipmentId_ImportCaseLineId",
                table: "shipment_line_allocations",
                columns: new[] { "ShipmentId", "ImportCaseLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_line_allocations_UpdatedByUserId",
                table: "shipment_line_allocations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_milestones_CreatedByUserId",
                table: "shipment_milestones",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_milestones_ShipmentId_SequenceNumber",
                table: "shipment_milestones",
                columns: new[] { "ShipmentId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_milestones_UpdatedByUserId",
                table: "shipment_milestones",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_status_histories_ChangedAtUtc",
                table: "shipment_status_histories",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_status_histories_ChangedByUserId",
                table: "shipment_status_histories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_status_histories_ImportCaseId",
                table: "shipment_status_histories",
                column: "ImportCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_status_histories_ShipmentId",
                table: "shipment_status_histories",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_CreatedByUserId",
                table: "shipments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Etd",
                table: "shipments",
                column: "Etd");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Eta",
                table: "shipments",
                column: "Eta");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_ImportCaseId_ShipmentSequence",
                table: "shipments",
                columns: new[] { "ImportCaseId", "ShipmentSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_ShipmentNumber",
                table: "shipments",
                column: "ShipmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Status",
                table: "shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_UpdatedByUserId",
                table: "shipments",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "document_number_counters");
            migrationBuilder.DropTable(name: "operation_idempotency_requests");
            migrationBuilder.DropTable(name: "shipment_containers");
            migrationBuilder.DropTable(name: "shipment_line_allocations");
            migrationBuilder.DropTable(name: "shipment_milestones");
            migrationBuilder.DropTable(name: "shipment_status_histories");
            migrationBuilder.DropTable(name: "import_case_lines");
            migrationBuilder.DropTable(name: "shipments");
            migrationBuilder.DropTable(name: "import_cases");
        }
    }
}
