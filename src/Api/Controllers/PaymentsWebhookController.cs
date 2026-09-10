using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShippingSystem.Api.Contracts;
using ShippingSystem.Application.Orders.Commands.ConfirmOrderPayment;

namespace ShippingSystem.Api.Controllers;

/// <summary>
/// FR-4.7, FR-11.4. Anonymous by necessity — the payment gateway calling this endpoint has
/// no user JWT to present. This is NOT a substitute for real webhook security: a production
/// deployment must verify the gateway's request signature (e.g. an HMAC header computed over
/// the raw body with a shared secret) before this controller ever calls Send — that
/// verification is gateway-SDK-specific and deliberately left as a TODO here since
/// SimulatedPaymentGatewayService (Infrastructure) has no real signature scheme to check
/// against. ConfirmOrderPaymentCommandHandler's own idempotency ledger check is what actually
/// makes retried/duplicate deliveries safe — signature verification only proves the request
/// came from the real gateway, it isn't the idempotency mechanism.
///
/// [DisableRateLimiting] — the global per-IP/per-user limiter (RateLimitingExtensions) is
/// tuned for browser/app clients, not a payment gateway's own retry policy, which this system
/// has no control over and must not silently drop. The real protection for this endpoint is
/// the signature verification above (once implemented), not a request-count throttle.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/payments")]
[AllowAnonymous]
[DisableRateLimiting]
public sealed class PaymentsWebhookController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentsWebhookController(ISender sender) => _sender = sender;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Receive(PaymentWebhookRequest request, CancellationToken cancellationToken)
    {
        // TODO before production: verify the gateway's signature header here and return 401
        // if it doesn't match, before this command ever runs.
        await _sender.Send(
            new ConfirmOrderPaymentCommand(
                request.OrderId,
                request.ProviderName,
                request.ExternalEventId,
                request.TransactionRef,
                request.Amount,
                request.IsSuccessful,
                request.ShippingFee,
                request.FailureReason),
            cancellationToken);

        // 204 regardless of business outcome — gateways generally only care that the
        // callback was received, not what it triggered internally; retries are driven by
        // the gateway's own delivery/timeout policy, not by inspecting this response body.
        return NoContent();
    }
}
