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

        // PurchaseOrders Table
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

        // PurchaseOrderLines Table
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
    }
}
