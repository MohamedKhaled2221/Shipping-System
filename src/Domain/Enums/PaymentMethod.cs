namespace ShippingSystem.Domain.Enums;

/// <summary>
/// Drives FR-4.3 (prepaid) vs FR-4.4 (COD) shipment-progression rules.
/// Card/Wallet/BankTransfer are all "prepaid" for business-rule purposes;
/// only CashOnDelivery gets the relaxed rule.
/// </summary>
public enum PaymentMethod
{
    CashOnDelivery = 0,
    Card = 1,
    Wallet = 2,
    BankTransfer = 3
}
