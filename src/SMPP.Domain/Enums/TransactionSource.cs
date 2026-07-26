namespace SMPP.Domain.Enums;

public enum TransactionSource
{
    ManualAdjustment = 0,
    QuickSend = 1,
    BulkSend = 2,
    BulkTemplateSend = 3,
    PublicApi = 4,
    PaymentApproval = 5
}
