# Durum Modeli

## Ticari durum
Imported, Confirmed, ProformaPending, ProformaApproved, ProductionPlanned, InProduction, ProductionCompleted, ReadyToShip, Closed, Cancelled

## Lojistik durum
NotStarted, ForwarderRequested, BookingPending, Booked, PickedUp, AtOriginPort, LoadedOnVessel, InTransit, Transshipment, AtDestinationPort, Discharged, InlandTransport, Delivered

## Gümrük/kabul durumu
NotStarted, DocumentsPending, DocumentsReady, DeclarationOpened, CustomsReview, TaxesCompleted, CustomsReleased, WarehouseReceived, QualityCompleted, ErpReceiptCompleted, Closed

## Kurallar

- Durum değişiklikleri timeline olayı oluşturur.
- Geri alma işlemi açıklama gerektirir.
- Bazı geçişler zorunlu alanlara bağlıdır.
- Sistem otomatik ve manuel olayları ayırır.
- İptal ve kapanış işlemleri özel yetki ister.
