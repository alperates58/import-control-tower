# Domain Modeli

## İlişki kuralları

- PurchaseOrder 1-N PurchaseOrderLine
- ImportCase N-N PurchaseOrder
- ImportCase 1-N Shipment
- Shipment N-N PurchaseOrderLine, ara tablo ShipmentAllocation ve miktar ile
- Shipment 1-N Container
- Container N-1 VesselVoyage (zaman içinde değişim için event/history destekli)
- Her ana varlık 1-N Document, Task, WorkflowEvent, AuditLog ilişkisine sahip olabilir

## Kritik invariants

- Allocation toplamı açık sipariş miktarını aşamaz.
- Konteyner numarası ISO 6346 kontrolünden geçirilebilir.
- Gerçek tarih girildiğinde ilgili planlanan tarih silinmez.
- Kapanan dosya yalnızca özel yetkiyle yeniden açılır.
