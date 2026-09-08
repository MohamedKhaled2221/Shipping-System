namespace ShippingSystem.Domain.Services;

/// <summary>
/// FR-9.1 — generates unique tracking numbers in the format TRK-YYYY-NNNNNN.
/// The Domain only depends on this abstraction; the actual sequence/counter
/// implementation (e.g. backed by a SQL Server table with HOLDLOCK, or Redis INCR)
/// lives in Infrastructure. Deliberately synchronous — see
/// Infrastructure.Services.TrackingNumberGenerator's own doc comment for why its
/// dedicated-DbContext-per-call design does its work without exposing a Task here.
/// </summary>
public interface ITrackingNumberGenerator
{
    string Generate(int year);
}
