namespace VigiaPlatform.API.SiteReceptionAndVerification.Infrastructure.Persistence.EntityFrameworkCore.Seeding;

using VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.Aggregates;
using VigiaPlatform.API.SiteReceptionAndVerification.Domain.Model.ValueObjects;

/// <summary>
/// Seeds initial development data for the Site Reception and Verification Bounded Context (BC-07).
/// Provides realistic instances for:
/// 1. An upcoming arrival at site gate (UnitArrived)
/// 2. A historical closed reception with full compliance (ConformantClosed)
/// 3. A reception with reported discrepancy and photographic evidence (DiscrepancyReported)
/// </summary>
public static class SiteReceptionSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Set<Reception>().Any())
        {
            return;
        }

        // Scenario 1: Upcoming arrival awaiting confirmation and physical verification at site
        var arrival1 = new Reception(
            dispatchId: "DSP-8001",
            carrierPlate: "V3B-920",
            driverName: "Carlos Mendoza",
            siteId: 1,
            siteName: "Obra Torres del Sol - Torre B",
            siteResidentName: "Ing. Valeria Rivas",
            arrivalTime: DateTimeOffset.UtcNow.AddMinutes(-45)
        );
        arrival1.AddItem("MAT-CEM-01", "Cemento Portland Tipo I (Bolsa 42.5kg)", "BOL", 500);
        arrival1.AddItem("MAT-VAR-12", "Varilla de Acero Corrugado 1/2 pulgada", "UND", 200);

        // Scenario 2: Fully compliant historical reception with weighbridge metrics and digital signature
        var arrival2 = new Reception(
            dispatchId: "DSP-8000",
            carrierPlate: "A1X-750",
            driverName: "Manuel Ramos",
            siteId: 1,
            siteName: "Obra Torres del Sol - Torre B",
            siteResidentName: "Ing. Valeria Rivas",
            arrivalTime: DateTimeOffset.UtcNow.AddHours(-6)
        );
        arrival2.AddItem("MAT-HOR-04", "Concreto Premezclado f'c=280 kg/cm2", "M3", 8.0);
        arrival2.ConfirmReceipt(
            deliveryNoteNumber: "GR-2026-004510",
            scaleWeight: new ScaleWeight(32000, 8000, "BAL-LIMA-9750", DateTimeOffset.UtcNow.AddHours(-5).AddMinutes(-40)),
            checklist: new ReceptionChecklist(
                isSealIntact: true,
                isDeliveryNotePhysicalOk: true,
                isScaleCalibrated: true,
                remarks: "Precinto de seguridad intacto N° S-99100. Guía física conforme."
            ),
            digitalSignature: new DigitalSignature(
                "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxMDAiIGhlaWdodD0iMzAiPjxwYXRoIGQ9Ik0xMCAxNSBRIDUwIDUgOTAgMTUiIHN0cm9rZT0iYmxhY2siIGZpbGw9InRyYW5zcGFyZW50Ii8+PC9zdmc+",
                "Ing. Valeria Rivas - Residente de Obra CIP 184920",
                DateTimeOffset.UtcNow.AddHours(-5).AddMinutes(-35)
            ),
            receiverNotes: "Despacho recibido sin observaciones en garita."
        );
        arrival2.VerifyMaterial("MAT-HOR-04", 8.0, "Prueba de asentamiento (slump) 4 pulgadas conforme según ASTM C143.");
        arrival2.RecordConformity("Conformidad total aprobada. Probeta de ensayo tomada para control de rotura a los 28 días.");

        // Scenario 3: Discrepancy flow with broken seal, shortage and photographic evidence
        var arrival3 = new Reception(
            dispatchId: "DSP-8002",
            carrierPlate: "B9F-841",
            driverName: "Jorge Alarcón",
            siteId: 1,
            siteName: "Obra Torres del Sol - Torre B",
            siteResidentName: "Ing. Valeria Rivas",
            arrivalTime: DateTimeOffset.UtcNow.AddHours(-3)
        );
        arrival3.AddItem("MAT-CEM-01", "Cemento Portland Tipo I (Bolsa 42.5kg)", "BOL", 400);
        arrival3.ConfirmReceipt(
            deliveryNoteNumber: "GR-2026-004522",
            scaleWeight: new ScaleWeight(21800, 6900, "BAL-LIMA-9825", DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(-50)),
            checklist: new ReceptionChecklist(
                isSealIntact: false,
                isDeliveryNotePhysicalOk: true,
                isScaleCalibrated: true,
                remarks: "Alerta: Precinto de seguridad N° S-99250 presenta rotura y manipulación previa."
            ),
            digitalSignature: new DigitalSignature(
                "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik0wIDAgTDEwMCAxMCIgc3Ryb2tlPSJyZWQiLz48L3N2Zz4=",
                "Ing. Valeria Rivas - Residente de Obra",
                DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(-45)
            ),
            receiverNotes: "Se realiza apertura supervisada debido al daño en precinto."
        );
        arrival3.VerifyMaterial("MAT-CEM-01", 365.0, "Faltante físico de 35 bolsas respecto a las 400 despachadas. Presencia de sacos rotos en plataforma.");
        arrival3.ReportDiscrepancy(
            reason: "MISSING_AND_DAMAGED_MATERIAL",
            description: "Faltante de 35 bolsas de cemento Portland y 12 bolsas humedecidas no aptas para vaciado estructural.",
            severity: "Critical"
        );
        arrival3.AttachEvidence(
            evidenceType: "BROKEN_SEAL_PHOTO",
            fileUrl: "https://storage.vigia.pe/evidences/reception-2/broken-seal-gate.jpg",
            description: "Foto en garita del precinto de seguridad fracturado y manipulado."
        );
        arrival3.AttachEvidence(
            evidenceType: "DAMAGED_MATERIAL_PHOTO",
            fileUrl: "https://storage.vigia.pe/evidences/reception-2/damaged-cement-bags.jpg",
            description: "Bolsas de cemento rotas con pérdida de material sobre la tolva del camión."
        );

        context.Set<Reception>().AddRange(arrival1, arrival2, arrival3);
        context.SaveChanges();
    }
}
