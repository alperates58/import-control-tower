using System;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ImportControlTower.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.13")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ApplicationRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<bool>("IsSystemRole")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("IX_roles_NormalizedName");

                    b.ToTable("roles", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<int>("AuthVersion")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("LastLoginUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("MustChangePassword")
                        .HasColumnType("boolean");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .IsUnique()
                        .HasDatabaseName("IX_users_NormalizedEmail");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("IX_users_NormalizedUserName");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.AuditLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid?>("ActorUserId")
                        .HasColumnType("uuid");

                    b.Property<string>("ActorUsername")
                        .HasColumnType("text");

                    b.Property<string>("ActorType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("EntityId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MetadataJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("TimestampUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UserAgent")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ActorUserId");

                    b.HasIndex("TimestampUtc");

                    b.ToTable("audit_logs", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.Permission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<string>("GroupName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("permissions", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CreatedByIp")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("ExpiresAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("FamilyId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsRevoked")
                        .HasColumnType("boolean");

                    b.Property<string>("ReplacedByTokenHash")
                        .HasColumnType("text");

                    b.Property<DateTime?>("RevokedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<uint>("Xmin")
                        .IsRowVersion()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("oid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("FamilyId");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("refresh_tokens", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.RolePermission", b =>
                {
                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("PermissionId")
                        .HasColumnType("uuid");

                    b.HasKey("RoleId", "PermissionId");

                    b.HasIndex("PermissionId");

                    b.ToTable("role_permissions", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.SystemSetting", b =>
                {
                    b.Property<string>("Key")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsSensitive")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("UpdatedByUserId")
                        .HasColumnType("uuid");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ValueType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.HasKey("Key");

                    b.HasIndex("UpdatedByUserId");

                    b.ToTable("system_settings", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("role_claims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("user_claims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("user_logins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("user_roles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("user_tokens", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.AuditLog", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", "ActorUser")
                        .WithMany("AuditLogs")
                        .HasForeignKey("ActorUserId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("ActorUser");
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.RefreshToken", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", "User")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.RolePermission", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.Permission", "Permission")
                        .WithMany("RolePermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationRole", "Role")
                        .WithMany("RolePermissions")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Permission");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.SystemSetting", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", "UpdatedByUser")
                        .WithMany()
                        .HasForeignKey("UpdatedByUserId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("UpdatedByUser");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("ImportControlTower.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ApplicationRole", b =>
                {
                    b.Navigation("RolePermissions");
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ApplicationUser", b =>
                {
                    b.Navigation("AuditLogs");

                    b.Navigation("RefreshTokens");
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ImportBatch", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CompletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("ConfirmedByUserId")
                        .HasColumnType("uuid");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("FailureReason")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("FileSha256")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<long>("FileSizeBytes")
                        .HasColumnType("bigint");

                    b.Property<int>("ImportedLineCount")
                        .HasColumnType("integer");

                    b.Property<int>("ImportedOrderCount")
                        .HasColumnType("integer");

                    b.Property<int>("InvalidRowCount")
                        .HasColumnType("integer");

                    b.Property<string>("OriginalFileName")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<string>("ParserVersion")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<DateTime>("StartedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("TemplateVersion")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<int>("TotalRowCount")
                        .HasColumnType("integer");

                    b.Property<Guid>("UploadedByUserId")
                        .HasColumnType("uuid");

                    b.Property<int>("ValidRowCount")
                        .HasColumnType("integer");

                    b.Property<int>("WarningRowCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ConfirmedByUserId");

                    b.HasIndex("FileSha256");

                    b.HasIndex("Status");

                    b.HasIndex("UploadedByUserId");

                    b.ToTable("import_batches", null, t =>
                        {
                            t.HasCheckConstraint("chk_import_batch_status", "status IN ('Uploaded','Parsing','MappingRequired','Validating','ValidationFailed','ReadyForConfirmation','Importing','Completed','Failed','Cancelled')");
                        });
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ImportBatchRow", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ErrorCodesJson")
                        .HasColumnType("jsonb");

                    b.Property<string>("ImportAction")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<Guid>("ImportBatchId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("MatchedLineId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("MatchedOrderId")
                        .HasColumnType("uuid");

                    b.Property<string>("NormalizedDataJson")
                        .HasColumnType("jsonb");

                    b.Property<string>("RawDataJson")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<int>("RowNumber")
                        .HasColumnType("integer");

                    b.Property<string>("ValidationStatus")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("WarningCodesJson")
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.HasIndex("MatchedLineId");

                    b.HasIndex("MatchedOrderId");

                    b.HasIndex("ValidationStatus");

                    b.HasIndex("ImportBatchId", "RowNumber")
                        .IsUnique();

                    b.ToTable("import_batch_rows", null, t =>
                        {
                            t.HasCheckConstraint("chk_batch_row_action", "import_action IN ('CreateOrder','CreateLine','SkipDuplicate','Conflict','Invalid')");

                            t.HasCheckConstraint("chk_batch_row_status", "validation_status IN ('Valid','Warning','Error')");
                        });
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.ImportConfirmationRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CompletedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("IdempotencyKey")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid>("ImportBatchId")
                        .HasColumnType("uuid");

                    b.Property<string>("ResponseJson")
                        .HasColumnType("jsonb");

                    b.Property<int?>("ResponseStatusCode")
                        .HasColumnType("integer");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.HasKey("Id");

                    b.HasIndex("ImportBatchId", "IdempotencyKey")
                        .IsUnique();

                    b.ToTable("import_confirmation_requests", null, t =>
                        {
                            t.HasCheckConstraint("chk_confirm_req_status", "status IN ('Processing','Completed','Failed')");
                        });
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.PurchaseOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("CreatedByUserId")
                        .HasColumnType("uuid");

                    b.Property<string>("NormalizedOrderNumber")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("NormalizedSupplierName")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<DateTime>("OrderDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("SupplierName")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("UpdatedByUserId")
                        .HasColumnType("uuid");

                    b.Property<uint>("Xmin")
                        .IsRowVersion()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("oid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("CreatedByUserId");

                    b.HasIndex("OrderDate");

                    b.HasIndex("Status");

                    b.HasIndex("UpdatedByUserId");

                    b.HasIndex("NormalizedOrderNumber", "NormalizedSupplierName")
                        .IsUnique();

                    b.ToTable("purchase_orders", (string)null);
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.PurchaseOrderLine", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("LineNumber")
                        .HasColumnType("integer");

                    b.Property<string>("NormalizedStockCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<decimal>("OrderedQuantity")
                        .HasPrecision(18, 4)
                        .HasColumnType("numeric(18,4)");

                    b.Property<Guid>("PurchaseOrderId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("RemainingQuantity")
                        .HasPrecision(18, 4)
                        .HasColumnType("numeric(18,4)");

                    b.Property<DateTime?>("SasDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("StockCode")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("StockName")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<uint>("Xmin")
                        .IsRowVersion()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("oid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedStockCode");

                    b.HasIndex("PurchaseOrderId", "NormalizedStockCode")
                        .IsUnique();

                    b.ToTable("purchase_order_lines", null, t =>
                        {
                            t.HasCheckConstraint("chk_po_line_quantities", "remaining_quantity >= 0 AND ordered_quantity > 0 AND remaining_quantity <= ordered_quantity");
                        });
                });

            modelBuilder.Entity("ImportControlTower.Domain.Entities.Permission", b =>
                {
                    b.Navigation("RolePermissions");
                });
#pragma warning restore 612, 618
        }
    }
}
