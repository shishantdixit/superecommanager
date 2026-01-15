namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Payment methods supported.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Cash on Delivery</summary>
    COD = 1,

    /// <summary>Prepaid - Credit/Debit Card</summary>
    Card = 2,

    /// <summary>Prepaid - UPI</summary>
    UPI = 3,

    /// <summary>Prepaid - Net Banking</summary>
    NetBanking = 4,

    /// <summary>Prepaid - Wallet (Paytm, PhonePe, etc.)</summary>
    Wallet = 5,

    /// <summary>EMI/BNPL</summary>
    EMI = 6,

    /// <summary>Bank Transfer</summary>
    BankTransfer = 7,

    /// <summary>Other/Unknown prepaid</summary>
    Other = 99
}
