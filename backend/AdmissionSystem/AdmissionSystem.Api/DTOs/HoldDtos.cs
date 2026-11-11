namespace AdmissionSystem.Api.DTOs;

// ── Responses ───────────────────────────────────────────────
public sealed record HoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt);

public sealed record HoldStatusResponse(
    string HoldId,
    string ClassId,
    string Status,
    DateTime ExpiresAt);

public sealed record ConfirmHoldResponse(
    string HoldId,
    string Status);

public sealed record MyHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt);
