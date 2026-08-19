namespace Application.Core.DTOs.Appointment.Request;

/// <summary>Resolves a PendientePago appointment's payment. Null InsuranceCode means "particular" (no insurance).</summary>
public sealed record ConfirmPaymentRequest(Guid? InsuranceCode = null);
