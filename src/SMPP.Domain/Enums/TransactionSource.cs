namespace SMPP.Domain.Enums;

public enum TransactionSource
{
    ManualAdjustment = 0,
    QuickSend = 1,
    BulkSend = 2,
    PublicApi = 3,
    PaymentApproval = 4
}
