namespace Infrastructure.Core.Interfaces.Audit;

/// <summary>
/// Request-scoped identity of the caller, used to stamp audited database writes.
/// </summary>
public interface IUserContext
{
    /// <summary>The <c>sub</c> claim of the JWT, or <c>null</c> for anonymous/system callers.</summary>
    Guid? UserCode { get; }

    /// <summary>
    /// The <c>patient_code</c> claim, present only on patient (mobile) tokens. Patients are not <c>User</c> rows and
    /// their <c>sub</c> is the e-mail, so <see cref="UserCode"/> is <c>null</c> for them.
    /// </summary>
    Guid? PatientCode { get; }

    /// <summary>The role claim of the JWT (<c>Administrador</c>, <c>Doctor</c>, <c>Enfermero</c>, <c>Patient</c>), if any.</summary>
    string? Role { get; }

    /// <summary>Remote IP address of the request, if available.</summary>
    string? IpAddress { get; }

    /// <summary>Correlation id of the request (<c>HttpContext.TraceIdentifier</c>).</summary>
    string? RequestId { get; }
}
