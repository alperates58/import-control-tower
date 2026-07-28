# Yetkilendirme

RBAC + permission tabanlı kontrol.

Örnek izinler:

- orders.view, orders.import, orders.edit
- importcases.view, importcases.create, importcases.edit, importcases.close
- shipments.view, shipments.edit
- containers.view, containers.edit
- documents.view, documents.upload, documents.delete
- tasks.view_all, tasks.assign, tasks.complete
- financial.view, financial.edit
- users.manage, roles.manage, settings.manage
- audit.view

Frontend gizleme tek başına güvenlik değildir; tüm kontroller API'de uygulanır.
