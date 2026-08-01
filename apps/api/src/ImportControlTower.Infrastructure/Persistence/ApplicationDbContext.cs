using ImportControlTower.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<SystemMigrationHistory> SystemMigrations => Set<SystemMigrationHistory>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportBatchRow> ImportBatchRows => Set<ImportBatchRow>();
    public DbSet<ImportConfirmationRequest> ImportConfirmationRequests => Set<ImportConfirmationRequest>();

    // Phase 03 DbSets
    public DbSet<DocumentNumberCounter> DocumentNumberCounters => Set<DocumentNumberCounter>();
    public DbSet<OperationIdempotencyRequest> OperationIdempotencyRequests => Set<OperationIdempotencyRequest>();
    public DbSet<ImportCase> ImportCases => Set<ImportCase>();
    public DbSet<ImportCaseLine> ImportCaseLines => Set<ImportCaseLine>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentLineAllocation> ShipmentLineAllocations => Set<ShipmentLineAllocation>();
    public DbSet<ShipmentContainer> ShipmentContainers => Set<ShipmentContainer>();
    public DbSet<ShipmentMilestone> ShipmentMilestones => Set<ShipmentMilestone>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistories => Set<ShipmentStatusHistory>();

    // Phase 04 DbSets
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // System Migration History Table
        builder.Entity<SystemMigrationHistory>(entity =>
        {
            entity.ToTable("system_migrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MigrationName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AppliedAtUtc).IsRequired();
        });

        // Identity Tables Snake Case Mapping
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
            entity.HasIndex(u => u.NormalizedUserName).IsUnique();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.HasIndex(r => r.NormalizedName).IsUnique();
        });

        builder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("user_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("user_logins"));
        builder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("role_claims"));
        builder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("user_tokens"));

        // Permissions Table
        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(100).IsRequired();
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.GroupName).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(250).IsRequired();
        });

        // RolePermissions Table
        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshTokens Table
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).HasMaxLength(256).IsRequired();
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.FamilyId);
            entity.Property(rt => rt.Xmin).IsRowVersion();

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLogs Table
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            entity.Property(a => a.EntityType).HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.ActorType).HasMaxLength(50).IsRequired();
            entity.HasIndex(a => a.TimestampUtc);
            entity.HasIndex(a => a.ActorUserId);

            entity.HasOne(a => a.ActorUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SystemSettings Table
        builder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(s => s.Key);
            entity.Property(s => s.Key).HasMaxLength(100);
            entity.Property(s => s.Value).IsRequired();
            entity.Property(s => s.ValueType).HasMaxLength(50).IsRequired();

            entity.HasOne(s => s.UpdatedByUser)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PurchaseOrders Table (Phase 02 - Unchanged)
        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(po => po.Id);
            entity.Property(po => po.OrderNumber).HasMaxLength(100).IsRequired();
            entity.Property(po => po.NormalizedOrderNumber).HasMaxLength(100).IsRequired();
            entity.Property(po => po.SupplierName).HasMaxLength(250).IsRequired();
            entity.Property(po => po.NormalizedSupplierName).HasMaxLength(250).IsRequired();
            entity.Property(po => po.Status).HasMaxLength(50).IsRequired();
            entity.Property(po => po.Source).HasMaxLength(50).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(po => new { po.NormalizedOrderNumber, po.NormalizedSupplierName }).IsUnique();
            entity.HasIndex(po => po.OrderDate);
            entity.HasIndex(po => po.Status);

            entity.HasOne(po => po.CreatedByUser)
                .WithMany()
                .HasForeignKey(po => po.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(po => po.UpdatedByUser)
                .WithMany()
                .HasForeignKey(po => po.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PurchaseOrderLines Table (Phase 02 - Unchanged)
        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("purchase_order_lines");
            entity.HasKey(pol => pol.Id);
            entity.Property(pol => pol.StockCode).HasMaxLength(100).IsRequired();
            entity.Property(pol => pol.NormalizedStockCode).HasMaxLength(100).IsRequired();
            entity.Property(pol => pol.StockName).HasMaxLength(250).IsRequired();
            entity.Property(pol => pol.OrderedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(pol => pol.RemainingQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(pol => new { pol.PurchaseOrderId, pol.NormalizedStockCode }).IsUnique();
            entity.HasIndex(pol => pol.NormalizedStockCode);

            entity.ToTable(t => t.HasCheckConstraint("chk_po_line_quantities", "\"RemainingQuantity\" >= 0 AND \"OrderedQuantity\" > 0 AND \"RemainingQuantity\" <= \"OrderedQuantity\""));

            entity.HasOne(pol => pol.PurchaseOrder)
                .WithMany(po => po.Lines)
                .HasForeignKey(pol => pol.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ImportBatches Table
        builder.Entity<ImportBatch>(entity =>
        {
            entity.ToTable("import_batches");
            entity.HasKey(ib => ib.Id);
            entity.Property(ib => ib.OriginalFileName).HasMaxLength(250).IsRequired();
            entity.Property(ib => ib.FileSha256).HasMaxLength(64).IsRequired();
            entity.Property(ib => ib.Status).HasMaxLength(50).IsRequired();
            entity.Property(ib => ib.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(ib => ib.FailureReason).HasMaxLength(500);
            entity.Property(ib => ib.ParserVersion).HasMaxLength(20).IsRequired();
            entity.Property(ib => ib.TemplateVersion).HasMaxLength(20).IsRequired();

            entity.HasIndex(ib => ib.FileSha256);
            entity.HasIndex(ib => ib.Status);
            entity.HasIndex(ib => ib.UploadedByUserId);

            entity.ToTable(t => t.HasCheckConstraint("chk_import_batch_status", "\"Status\" IN ('Uploaded','Parsing','MappingRequired','Validating','ValidationFailed','ReadyForConfirmation','Importing','Completed','Failed','Cancelled')"));

            entity.HasOne(ib => ib.UploadedByUser)
                .WithMany()
                .HasForeignKey(ib => ib.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ib => ib.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(ib => ib.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ImportBatchRows Table
        builder.Entity<ImportBatchRow>(entity =>
        {
            entity.ToTable("import_batch_rows");
            entity.HasKey(ibr => ibr.Id);
            entity.Property(ibr => ibr.RawDataJson).HasColumnType("jsonb").IsRequired();
            entity.Property(ibr => ibr.NormalizedDataJson).HasColumnType("jsonb");
            entity.Property(ibr => ibr.ErrorCodesJson).HasColumnType("jsonb");
            entity.Property(ibr => ibr.WarningCodesJson).HasColumnType("jsonb");
            entity.Property(ibr => ibr.ValidationStatus).HasMaxLength(20).IsRequired();
            entity.Property(ibr => ibr.ImportAction).HasMaxLength(50).IsRequired();

            entity.HasIndex(ibr => new { ibr.ImportBatchId, ibr.RowNumber }).IsUnique();
            entity.HasIndex(ibr => ibr.ValidationStatus);

            entity.ToTable(t => t.HasCheckConstraint("chk_batch_row_status", "\"ValidationStatus\" IN ('Valid','Warning','Error')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_batch_row_action", "\"ImportAction\" IN ('CreateOrder','CreateLine','SkipDuplicate','Conflict','Invalid')"));

            entity.HasOne(ibr => ibr.ImportBatch)
                .WithMany(ib => ib.Rows)
                .HasForeignKey(ibr => ibr.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ibr => ibr.MatchedOrder)
                .WithMany()
                .HasForeignKey(ibr => ibr.MatchedOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(ibr => ibr.MatchedLine)
                .WithMany()
                .HasForeignKey(ibr => ibr.MatchedLineId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ImportConfirmationRequests Table
        builder.Entity<ImportConfirmationRequest>(entity =>
        {
            entity.ToTable("import_confirmation_requests");
            entity.HasKey(icr => icr.Id);
            entity.Property(icr => icr.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(icr => icr.Status).HasMaxLength(30).IsRequired();
            entity.Property(icr => icr.ResponseJson).HasColumnType("jsonb");

            entity.HasIndex(icr => new { icr.ImportBatchId, icr.IdempotencyKey }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("chk_confirm_req_status", "status IN ('Processing','Completed','Failed')"));

            entity.HasOne(icr => icr.ImportBatch)
                .WithMany(ib => ib.ConfirmationRequests)
                .HasForeignKey(icr => icr.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // PHASE 03 ENTITY CONFIGURATIONS
        // ==========================================

        // DocumentNumberCounters Table
        builder.Entity<DocumentNumberCounter>(entity =>
        {
            entity.ToTable("document_number_counters");
            entity.HasKey(c => new { c.DocumentType, c.Year });
            entity.Property(c => c.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Year).IsRequired();
            entity.Property(c => c.LastNumber).IsRequired();

            entity.ToTable(t => t.HasCheckConstraint("chk_doc_counter_values", "\"Year\" >= 2000 AND \"LastNumber\" >= 0"));
        });

        // OperationIdempotencyRequests Table
        builder.Entity<OperationIdempotencyRequest>(entity =>
        {
            entity.ToTable("operation_idempotency_requests");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.OperationType).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ScopeKey).HasMaxLength(100).IsRequired();
            entity.Property(r => r.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(30).IsRequired();
            entity.Property(r => r.ResponseJson).HasColumnType("jsonb");

            entity.HasIndex(r => new { r.RequestedByUserId, r.OperationType, r.ScopeKey, r.IdempotencyKey }).IsUnique();

            entity.HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("chk_idempotency_req_status", "\"Status\" IN ('Processing', 'Completed', 'Failed') AND (\"ResponseStatusCode\" IS NULL OR (\"ResponseStatusCode\" >= 100 AND \"ResponseStatusCode\" <= 599))"));
        });

        // ImportCases Table
        builder.Entity<ImportCase>(entity =>
        {
            entity.ToTable("import_cases");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CaseNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Status).HasMaxLength(50).IsRequired();
            entity.Property(c => c.SupplierName).HasMaxLength(250).IsRequired();
            entity.Property(c => c.NormalizedSupplierName).HasMaxLength(250).IsRequired();
            entity.Property(c => c.OriginCountry).HasMaxLength(100);
            entity.Property(c => c.DefaultTransportMode).HasMaxLength(30);
            entity.Property(c => c.Incoterm).HasMaxLength(10);
            entity.Property(c => c.ProductionStatus).HasMaxLength(30).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(c => c.CaseNumber).IsUnique();
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.NormalizedSupplierName);
            entity.HasIndex(c => c.ResponsibleUserId);
            entity.HasIndex(c => c.CreatedAtUtc);
            entity.HasIndex(c => c.UpdatedAtUtc);

            entity.ToTable(t => t.HasCheckConstraint("chk_import_case_status", "\"Status\" IN ('Draft', 'Active', 'Closed', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_import_case_prod_status", "\"ProductionStatus\" IN ('NotStarted', 'Started', 'Delayed', 'Completed', 'ReadyForShipment')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_import_case_default_mode", "\"DefaultTransportMode\" IS NULL OR \"DefaultTransportMode\" IN ('Sea', 'Air', 'Road', 'Rail', 'Courier', 'Multimodal')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_import_case_seq", "\"LastShipmentSequence\" >= 0"));

            entity.HasOne(c => c.ResponsibleUser)
                .WithMany()
                .HasForeignKey(c => c.ResponsibleUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.PurchasingOwnerUser)
                .WithMany()
                .HasForeignKey(c => c.PurchasingOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.OperationsOwnerUser)
                .WithMany()
                .HasForeignKey(c => c.OperationsOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.UpdatedByUser)
                .WithMany()
                .HasForeignKey(c => c.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ImportCaseLines Table
        builder.Entity<ImportCaseLine>(entity =>
        {
            entity.ToTable("import_case_lines");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.AllocatedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(l => l.ReleasedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(l => l.Status).HasMaxLength(30).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(l => new { l.ImportCaseId, l.PurchaseOrderLineId }).IsUnique();
            entity.HasIndex(l => new { l.Id, l.ImportCaseId }).IsUnique(); // For Composite FK

            entity.ToTable(t => t.HasCheckConstraint("chk_import_case_line_status", "\"Status\" IN ('Allocated', 'PartiallyShipped', 'FullyShipped', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_import_case_line_quantities", "\"AllocatedQuantity\" > 0 AND \"ReleasedQuantity\" >= 0 AND \"ReleasedQuantity\" <= \"AllocatedQuantity\""));

            entity.HasOne(l => l.ImportCase)
                .WithMany(c => c.Lines)
                .HasForeignKey(l => l.ImportCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.PurchaseOrderLine)
                .WithMany()
                .HasForeignKey(l => l.PurchaseOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.CreatedByUser)
                .WithMany()
                .HasForeignKey(l => l.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.UpdatedByUser)
                .WithMany()
                .HasForeignKey(l => l.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Shipments Table
        builder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ShipmentNumber).HasMaxLength(60).IsRequired();
            entity.Property(s => s.TransportMode).HasMaxLength(30).IsRequired();
            entity.Property(s => s.BookingNumber).HasMaxLength(100);
            entity.Property(s => s.OriginLocation).HasMaxLength(200).IsRequired();
            entity.Property(s => s.DestinationLocation).HasMaxLength(200).IsRequired();
            entity.Property(s => s.CarrierName).HasMaxLength(200);
            entity.Property(s => s.TransportReference).HasMaxLength(200);
            entity.Property(s => s.VesselName).HasMaxLength(200);
            entity.Property(s => s.VoyageNumber).HasMaxLength(100);
            entity.Property(s => s.OriginTimezoneId).HasMaxLength(100).IsRequired();
            entity.Property(s => s.DestinationTimezoneId).HasMaxLength(100).IsRequired();
            entity.Property(s => s.ModeSpecificMetadata).HasColumnType("jsonb");
            entity.Property(s => s.Status).HasMaxLength(50).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(s => s.ShipmentNumber).IsUnique();
            entity.HasIndex(s => new { s.ImportCaseId, s.ShipmentSequence }).IsUnique();
            entity.HasIndex(s => new { s.Id, s.ImportCaseId }).IsUnique(); // For Composite FK
            entity.HasIndex(s => s.Etd);
            entity.HasIndex(s => s.Eta);
            entity.HasIndex(s => s.Status);

            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_sequence", "\"ShipmentSequence\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_mode", "\"TransportMode\" IN ('Sea', 'Air', 'Road', 'Rail', 'Courier', 'Multimodal')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_status", "\"Status\" IN ('Draft', 'BookingPending', 'Booked', 'Loading', 'InTransit', 'Arrived', 'Delivered', 'Cancelled', 'Aborted')"));

            entity.HasOne(s => s.ImportCase)
                .WithMany(c => c.Shipments)
                .HasForeignKey(s => s.ImportCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.UpdatedByUser)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ShipmentLineAllocations Table
        builder.Entity<ShipmentLineAllocation>(entity =>
        {
            entity.ToTable("shipment_line_allocations");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AllocatedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(a => a.ReleasedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(a => a.ShippedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(a => a.ReceivedQuantity).HasPrecision(18, 4).IsRequired();
            entity.Property(a => a.Status).HasMaxLength(30).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(a => new { a.ShipmentId, a.ImportCaseLineId }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_line_alloc_status", "\"Status\" IN ('Allocated', 'Shipped', 'Received', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_line_alloc_quantities", "\"AllocatedQuantity\" > 0 AND \"ReleasedQuantity\" >= 0 AND \"ShippedQuantity\" >= 0 AND \"ReceivedQuantity\" >= 0 AND \"ShippedQuantity\" <= (\"AllocatedQuantity\" - \"ReleasedQuantity\") AND \"ReceivedQuantity\" <= \"ShippedQuantity\""));

            entity.HasOne(a => a.Shipment)
                .WithMany(s => s.LineAllocations)
                .HasForeignKey(a => a.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ImportCaseLine)
                .WithMany(l => l.ShipmentAllocations)
                .HasForeignKey(a => a.ImportCaseLineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ImportCase)
                .WithMany()
                .HasForeignKey(a => a.ImportCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.UpdatedByUser)
                .WithMany()
                .HasForeignKey(a => a.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Composite Foreign Keys for Same-Case Guarantee
            entity.HasOne<Shipment>()
                .WithMany()
                .HasForeignKey(a => new { a.ShipmentId, a.ImportCaseId })
                .HasPrincipalKey(s => new { s.Id, s.ImportCaseId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ImportCaseLine>()
                .WithMany()
                .HasForeignKey(a => new { a.ImportCaseLineId, a.ImportCaseId })
                .HasPrincipalKey(l => new { l.Id, l.ImportCaseId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ShipmentContainers Table
        builder.Entity<ShipmentContainer>(entity =>
        {
            entity.ToTable("shipment_containers");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ContainerNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.NormalizedContainerNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.ContainerType).HasMaxLength(30).IsRequired();
            entity.Property(c => c.SealNumber).HasMaxLength(50);
            entity.Property(c => c.GrossWeightKg).HasPrecision(12, 2);
            entity.Property(c => c.NetWeightKg).HasPrecision(12, 2);
            entity.Property(c => c.Status).HasMaxLength(30).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(c => c.NormalizedContainerNumber);
            entity.HasIndex(c => c.ShipmentId);

            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_container_status", "\"Status\" IN ('Assigned', 'Loaded', 'InTransit', 'Discharged', 'Delivered', 'Returned', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_container_type", "\"ContainerType\" IN ('20GP', '40GP', '40HC', '45HC', 'LCL', 'Reefer', 'OpenTop', 'FlatRack', 'Other')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_shipment_container_weights", "(\"GrossWeightKg\" IS NULL OR \"GrossWeightKg\" > 0) AND (\"NetWeightKg\" IS NULL OR \"NetWeightKg\" >= 0) AND (\"GrossWeightKg\" IS NULL OR \"NetWeightKg\" IS NULL OR \"NetWeightKg\" <= \"GrossWeightKg\") AND (\"PackageCount\" IS NULL OR \"PackageCount\" > 0)"));

            entity.HasOne(c => c.Shipment)
                .WithMany(s => s.Containers)
                .HasForeignKey(c => c.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.UpdatedByUser)
                .WithMany()
                .HasForeignKey(c => c.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ShipmentMilestones Table
        builder.Entity<ShipmentMilestone>(entity =>
        {
            entity.ToTable("shipment_milestones");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.MilestoneType).HasMaxLength(60).IsRequired();
            entity.Property(m => m.LocationName).HasMaxLength(200);
            entity.Property(m => m.TimezoneId).HasMaxLength(100).IsRequired();
            entity.Property(m => m.Status).HasMaxLength(30).IsRequired();
            entity.Property(m => m.Source).HasMaxLength(30).IsRequired();
            entity.Property<uint>("xmin").IsRowVersion();

            entity.HasIndex(m => new { m.ShipmentId, m.SequenceNumber }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("chk_milestone_seq", "\"SequenceNumber\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("chk_milestone_status", "\"Status\" IN ('Pending', 'InProgress', 'Completed', 'Skipped', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_milestone_source", "\"Source\" IN ('Manual', 'SystemDerived')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_milestone_completed_date", "\"Status\" != 'Completed' OR \"ActualAtUtc\" IS NOT NULL"));

            entity.HasOne(m => m.Shipment)
                .WithMany(s => s.Milestones)
                .HasForeignKey(m => m.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.UpdatedByUser)
                .WithMany()
                .HasForeignKey(m => m.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ShipmentStatusHistories Table
        builder.Entity<ShipmentStatusHistory>(entity =>
        {
            entity.ToTable("shipment_status_histories");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.EntityType).HasMaxLength(30).IsRequired();
            entity.Property(h => h.OldStatus).HasMaxLength(50);
            entity.Property(h => h.NewStatus).HasMaxLength(50).IsRequired();
            entity.Property(h => h.Reason).HasMaxLength(250);

            entity.HasIndex(h => h.ImportCaseId);
            entity.HasIndex(h => h.ShipmentId);
            entity.HasIndex(h => h.ChangedAtUtc);

            entity.ToTable(t => t.HasCheckConstraint("chk_status_history_entity_ref", "(\"EntityType\" = 'ImportCase' AND \"ImportCaseId\" IS NOT NULL AND \"ShipmentId\" IS NULL) OR (\"EntityType\" = 'Shipment' AND \"ShipmentId\" IS NOT NULL AND \"ImportCaseId\" IS NULL)"));

            entity.HasOne(h => h.ImportCase)
                .WithMany()
                .HasForeignKey(h => h.ImportCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Shipment)
                .WithMany()
                .HasForeignKey(h => h.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Documents Table
        builder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(d => d.Title).HasMaxLength(200).IsRequired();
            entity.Property(d => d.DocumentNumber).HasMaxLength(100);
            entity.Property(d => d.Status).HasMaxLength(30).IsRequired();

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(d => d.ImportCaseId);
            entity.HasIndex(d => d.ShipmentId);
            entity.HasIndex(d => d.ShipmentContainerId);

            entity.ToTable(t => t.HasCheckConstraint("chk_documents_exact_one_scope", "(\"ImportCaseId\" IS NOT NULL AND \"ShipmentId\" IS NULL AND \"ShipmentContainerId\" IS NULL) OR (\"ImportCaseId\" IS NULL AND \"ShipmentId\" IS NOT NULL AND \"ShipmentContainerId\" IS NULL) OR (\"ImportCaseId\" IS NULL AND \"ShipmentId\" IS NULL AND \"ShipmentContainerId\" IS NOT NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("chk_documents_status", "\"Status\" IN ('Active', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_documents_dates", "\"ExpiryDate\" IS NULL OR \"DocumentDate\" IS NULL OR \"ExpiryDate\" >= \"DocumentDate\""));

            entity.HasOne(d => d.ImportCase)
                .WithMany()
                .HasForeignKey(d => d.ImportCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Shipment)
                .WithMany()
                .HasForeignKey(d => d.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ShipmentContainer)
                .WithMany()
                .HasForeignKey(d => d.ShipmentContainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UpdatedByUser)
                .WithMany()
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DocumentVersions Table
        builder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(v => v.StoredObjectKey).HasMaxLength(500).IsRequired();
            entity.Property(v => v.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(v => v.FileExtension).HasMaxLength(20).IsRequired();
            entity.Property(v => v.Sha256Hash).HasMaxLength(64).IsRequired();
            entity.Property(v => v.StorageStatus).HasMaxLength(30).IsRequired();
            entity.Property(v => v.Status).HasMaxLength(30).IsRequired();

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
            entity.HasIndex(v => v.DocumentId)
                .HasDatabaseName("ux_document_versions_one_current")
                .HasFilter("\"IsCurrent\" = true AND \"Status\" = 'Active' AND \"StorageStatus\" = 'Active'")
                .IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("chk_document_versions_size", "\"FileSizeBytes\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("chk_document_versions_ver", "\"VersionNumber\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("chk_document_versions_hash", "length(\"Sha256Hash\") = 64"));
            entity.ToTable(t => t.HasCheckConstraint("chk_document_versions_status", "\"Status\" IN ('Active', 'Replaced', 'Cancelled')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_document_versions_storage_status", "\"StorageStatus\" IN ('Pending', 'Active', 'Failed', 'CleanupRequired')"));

            entity.HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.UploadedByUser)
                .WithMany()
                .HasForeignKey(v => v.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DocumentRequirements Table
        builder.Entity<DocumentRequirement>(entity =>
        {
            entity.ToTable("document_requirements");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ScopeType).HasMaxLength(30).IsRequired();
            entity.Property(r => r.TransportMode).HasMaxLength(30);
            entity.Property(r => r.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(255);

            entity.HasIndex(r => new { r.ScopeType, r.TransportMode, r.DocumentType }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("chk_document_requirements_scope", "\"ScopeType\" IN ('ImportCase', 'Shipment')"));
            entity.ToTable(t => t.HasCheckConstraint("chk_document_requirements_sort", "\"SortOrder\" >= 0"));
        });
    }
}
