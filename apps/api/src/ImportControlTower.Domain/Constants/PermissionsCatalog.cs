namespace ImportControlTower.Domain.Constants;

public static class PermissionsCatalog
{
    // 1. Dashboard (1)
    public const string DashboardView = "dashboard.view";

    // 2. Purchase Orders (3)
    public const string PurchaseOrdersView = "purchaseorders.view";
    public const string PurchaseOrdersImport = "purchaseorders.import";
    public const string PurchaseOrdersEdit = "purchaseorders.edit";

    // 3. Import Cases (4)
    public const string ImportCasesView = "importcases.view";
    public const string ImportCasesCreate = "importcases.create";
    public const string ImportCasesEdit = "importcases.edit";
    public const string ImportCasesClose = "importcases.close";

    // 4. Shipments (2)
    public const string ShipmentsView = "shipments.view";
    public const string ShipmentsEdit = "shipments.edit";

    // 5. Containers (2)
    public const string ContainersView = "containers.view";
    public const string ContainersEdit = "containers.edit";

    // 6. Documents (3)
    public const string DocumentsView = "documents.view";
    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsDelete = "documents.delete";

    // 7. Tasks (4)
    public const string TasksViewOwn = "tasks.view_own";
    public const string TasksViewAll = "tasks.view_all";
    public const string TasksAssign = "tasks.assign";
    public const string TasksComplete = "tasks.complete";

    // 8. Users (4)
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDisable = "users.disable";

    // 9. Roles (4)
    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";

    // 10. Settings (2)
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";

    // 11. Audit (1)
    public const string AuditView = "audit.view";

    // 12. Financial (2)
    public const string FinancialView = "financial.view";
    public const string FinancialEdit = "financial.edit";

    public static readonly IReadOnlyList<PermissionItem> All = new List<PermissionItem>
    {
        new(DashboardView, "Dashboard", "Genel bakış ve KPI şeritlerini görüntüleme"),

        new(PurchaseOrdersView, "Satın Alma", "Satın alma siparişlerini görüntüleme"),
        new(PurchaseOrdersImport, "Satın Alma", "Excel'den sipariş aktarımı"),
        new(PurchaseOrdersEdit, "Satın Alma", "Sipariş detaylarını düzenleme"),

        new(ImportCasesView, "İthalat Dosyaları", "İthalat dosyalarını görüntüleme"),
        new(ImportCasesCreate, "İthalat Dosyaları", "Yeni ithalat dosyası açma"),
        new(ImportCasesEdit, "İthalat Dosyaları", "İthalat dosyası güncelleme"),
        new(ImportCasesClose, "İthalat Dosyaları", "İthalat dosyasını kapatma"),

        new(ShipmentsView, "Sevkiyatlar", "Sevkiyat durumlarını izleme"),
        new(ShipmentsEdit, "Sevkiyatlar", "Sevkiyat güncelleme"),

        new(ContainersView, "Konteynerler", "Konteyner takibi yapma"),
        new(ContainersEdit, "Konteynerler", "Konteyner veri girişi"),

        new(DocumentsView, "Belgeler", "İthalat evraklarını görüntüleme"),
        new(DocumentsUpload, "Belgeler", "Yeni evrak yükleme"),
        new(DocumentsDelete, "Belgeler", "Evrak silme"),

        new(TasksViewOwn, "Görevler", "Kendi görevlerini görüntüleme"),
        new(TasksViewAll, "Görevler", "Tüm görevleri görüntüleme"),
        new(TasksAssign, "Görevler", "Görev atama"),
        new(TasksComplete, "Görevler", "Görev tamamlama"),

        new(UsersView, "Kullanıcı Yönetimi", "Kullanıcı listesi ve detay görüntüleme"),
        new(UsersCreate, "Kullanıcı Yönetimi", "Yeni kullanıcı oluşturma"),
        new(UsersEdit, "Kullanıcı Yönetimi", "Kullanıcı bilgilerini düzenleme"),
        new(UsersDisable, "Kullanıcı Yönetimi", "Kullanıcıyı devre dışı bırakma / aktif etme"),

        new(RolesView, "Rol Yönetimi", "Rol listesini görüntüleme"),
        new(RolesCreate, "Rol Yönetimi", "Yeni rol oluşturma"),
        new(RolesEdit, "Rol Yönetimi", "Rol izinlerini güncelleme"),
        new(RolesDelete, "Rol Yönetimi", "Özel rolleri silme"),

        new(SettingsView, "Sistem Ayarları", "Sistem ayarlarını görüntüleme"),
        new(SettingsManage, "Sistem Ayarları", "Sistem ayarlarını (Feature flags vb.) güncelleme"),

        new(AuditView, "Audit Log", "Audit kayıtlarını inceleme"),

        new(FinancialView, "Finans", "Fiyat, masraf ve toplam tutarları görüntüleme"),
        new(FinancialEdit, "Finans", "Fiyat ve maliyet verilerini girme/güncelleme")
    }.AsReadOnly();
}

public record PermissionItem(string Code, string GroupName, string Description);
