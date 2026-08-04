using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Reports;
using SMPP.Domain.Enums;

namespace SMPP.Web.Controllers.Api;

public record EnumValueApiResponse(string Name, int Value);

public record HealthApiResponse(string Status, string Version, DateTime ServerTimeUtc);

/// <summary>
/// Reference data for integrators: a liveness probe and the numeric value of every enum the API
/// accepts or returns, so a client can map them without hard-coding numbers read off this
/// documentation.
/// </summary>
[AllowAnonymous]
[Route("api/v1")]
[Tags("Meta")]
public class MetaApiController : ApiControllerBase
{
    /// <summary>Liveness probe. Answers without touching the database.</summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthApiResponse), StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new HealthApiResponse(
        "ok",
        typeof(MetaApiController).Assembly.GetName().Version?.ToString() ?? "unknown",
        DateTime.UtcNow));

    /// <summary>Every enum the API uses, as name/value pairs.</summary>
    [HttpGet("enums")]
    [ProducesResponseType(typeof(IDictionary<string, IReadOnlyList<EnumValueApiResponse>>), StatusCodes.Status200OK)]
    public IActionResult Enums() => Ok(new Dictionary<string, IReadOnlyList<EnumValueApiResponse>>
    {
        ["userRole"] = Values<UserRole>(),
        ["messageSource"] = Values<MessageSource>(),
        ["messageStatus"] = Values<MessageStatus>(),
        ["campaignSourceType"] = Values<CampaignSourceType>(),
        ["paymentMethod"] = Values<PaymentMethod>(),
        ["paymentStatus"] = Values<PaymentStatus>(),
        ["transactionKind"] = Values<TransactionKind>(),
        ["transactionSource"] = Values<TransactionSource>(),
        ["spamKeywordType"] = Values<SpamKeywordType>(),
        ["reportType"] = Values<ReportType>(),
    });

    private static IReadOnlyList<EnumValueApiResponse> Values<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(v => new EnumValueApiResponse(v.ToString(), Convert.ToInt32(v)))
            .ToList();
}
