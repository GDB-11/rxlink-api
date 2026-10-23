using Application.Core.DTOs.Consent.Errors;
using Application.Core.DTOs.Consent.Request;
using Application.Core.DTOs.Consent.Response;
using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.PrivacyNotice.Request;
using Application.Core.DTOs.PrivacyNotice.Response;
using Application.Core.Interfaces.Consent;
using Application.Core.Interfaces.PrivacyNotice;
using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Patient.Response;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.PatientExport;
using Application.Core.DTOs.Amendment.Errors;
using Application.Core.DTOs.Amendment.Request;
using Application.Core.DTOs.Amendment.Response;
using Application.Core.DTOs.CommunicationPreference.Errors;
using Application.Core.DTOs.CommunicationPreference.Request;
using Application.Core.DTOs.CommunicationPreference.Response;
using Application.Core.DTOs.Disclosure.Errors;
using Application.Core.DTOs.Disclosure.Request;
using Application.Core.DTOs.Disclosure.Response;
using Application.Core.Interfaces.Amendment;
using Application.Core.Interfaces.CommunicationPreference;
using Application.Core.Interfaces.Disclosure;
using Application.Core.DTOs.PatientExport.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Patient")]
[ApiController]
[Route("api/v1/patient")]
public sealed class PatientMedicalController : FunctionalController
{
    private readonly IDiagnostic _diagnosticService;
    private readonly IPatient _patientService;
    private readonly IPatientExport _exportService;
    private readonly IAmendment _amendmentService;
    private readonly IDisclosure _disclosureService;
    private readonly IPrivacyNotice _privacyNoticeService;
    private readonly IConsent _consentService;
    private readonly IErrorHttpMapper<PrivacyNoticeError> _privacyNoticeErrorMapper;
    private readonly IErrorHttpMapper<ConsentError> _consentErrorMapper;
    private readonly ICommunicationPreference _preferenceService;
    private readonly IErrorHttpMapper<DiagnosticError> _diagnosticErrorMapper;
    private readonly IErrorHttpMapper<PatientError> _patientErrorMapper;
    private readonly IErrorHttpMapper<AmendmentError> _amendmentErrorMapper;
    private readonly IErrorHttpMapper<DisclosureError> _disclosureErrorMapper;
    private readonly IErrorHttpMapper<CommunicationPreferenceError> _preferenceErrorMapper;

    public PatientMedicalController(
        IDiagnostic diagnosticService,
        IPatient patientService,
        IPatientExport exportService,
        IAmendment amendmentService,
        IDisclosure disclosureService,
        ICommunicationPreference preferenceService,
        IPrivacyNotice privacyNoticeService,
        IConsent consentService,
        IErrorHttpMapper<PrivacyNoticeError> privacyNoticeErrorMapper,
        IErrorHttpMapper<ConsentError> consentErrorMapper,
        IErrorHttpMapper<DiagnosticError> diagnosticErrorMapper,
        IErrorHttpMapper<PatientError> patientErrorMapper,
        IErrorHttpMapper<AmendmentError> amendmentErrorMapper,
        IErrorHttpMapper<DisclosureError> disclosureErrorMapper,
        IErrorHttpMapper<CommunicationPreferenceError> preferenceErrorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _diagnosticService = diagnosticService;
        _patientService = patientService;
        _exportService = exportService;
        _amendmentService = amendmentService;
        _disclosureService = disclosureService;
        _privacyNoticeService = privacyNoticeService;
        _consentService = consentService;
        _privacyNoticeErrorMapper = privacyNoticeErrorMapper;
        _consentErrorMapper = consentErrorMapper;
        _diagnosticErrorMapper = diagnosticErrorMapper;
        _patientErrorMapper = patientErrorMapper;
        _amendmentErrorMapper = amendmentErrorMapper;
        _disclosureErrorMapper = disclosureErrorMapper;
        _preferenceService = preferenceService;
        _preferenceErrorMapper = preferenceErrorMapper;
    }

    /// <summary>Returns the authenticated patient's diagnostics with prescription summary.</summary>
    [HttpGet("diagnostics")]
    [ProducesResponseType(typeof(DiagnosticPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetDiagnostics([FromQuery] DiagnosticPageRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _diagnosticService.GetPageAsync(patientCode, request),
            errorMapper: _diagnosticErrorMapper,
            operationName: nameof(GetDiagnostics)
        );

    /// <summary>Returns the authenticated patient's own profile including allergies.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMe() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _patientService.GetSelfAsync(patientCode),
            errorMapper: _patientErrorMapper,
            operationName: nameof(GetMe)
        );

    /// <summary>Updates the authenticated patient's contact information.</summary>
    [HttpPatch("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateMe([FromBody] UpdatePatientSelfRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _patientService.UpdateSelfAsync(patientCode, request),
            errorMapper: _patientErrorMapper,
            operationName: nameof(UpdateMe),
            successMapper: _ => NoContent()
        );

    /// <summary>Returns a complete copy of the authenticated patient's own record (right of access).</summary>
    [HttpGet("me/export")]
    [ProducesResponseType(typeof(PatientExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ExportMe() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _exportService.ExportAsync(patientCode),
            errorMapper: _patientErrorMapper,
            operationName: nameof(ExportMe)
        );

    /// <summary>Submits a request to amend the authenticated patient's own record (right of amendment).</summary>
    [HttpPost("me/amendment-requests")]
    [ProducesResponseType(typeof(AmendmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> CreateAmendmentRequest([FromBody] CreateAmendmentRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _amendmentService.CreateAsync(patientCode, request),
            errorMapper: _amendmentErrorMapper,
            operationName: nameof(CreateAmendmentRequest),
            successMapper: amendment => Created(
                $"api/v1/patient/me/amendment-requests/{amendment.AmendmentRequestCode}", amendment)
        );

    /// <summary>The authenticated patient's amendment requests and their outcome, newest first.</summary>
    [HttpGet("me/amendment-requests")]
    [ProducesResponseType(typeof(AmendmentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyAmendmentRequests([FromQuery] AmendmentPageRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _amendmentService.GetOwnPageAsync(patientCode, request),
            errorMapper: _amendmentErrorMapper,
            operationName: nameof(GetMyAmendmentRequests)
        );

    /// <summary>Accounting of disclosures of the authenticated patient's information, newest first.</summary>
    [HttpGet("me/disclosures")]
    [ProducesResponseType(typeof(DisclosurePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyDisclosures([FromQuery] DisclosurePageRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _disclosureService.GetOwnPageAsync(patientCode, request),
            errorMapper: _disclosureErrorMapper,
            operationName: nameof(GetMyDisclosures)
        );

    /// <summary>The authenticated patient's channel restrictions and alternative contact.</summary>
    [HttpGet("me/communication-preferences")]
    [ProducesResponseType(typeof(CommunicationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyCommunicationPreferences() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _preferenceService.GetOwnAsync(patientCode),
            errorMapper: _preferenceErrorMapper,
            operationName: nameof(GetMyCommunicationPreferences)
        );

    /// <summary>Replaces the authenticated patient's channel restrictions and alternative contact.</summary>
    [HttpPut("me/communication-preferences")]
    [ProducesResponseType(typeof(CommunicationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateMyCommunicationPreferences([FromBody] UpdateCommunicationPreferenceRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _preferenceService.UpdateOwnAsync(patientCode, request),
            errorMapper: _preferenceErrorMapper,
            operationName: nameof(UpdateMyCommunicationPreferences)
        );

    /// <summary>The Notice of Privacy Practices in force and the authenticated patient's acknowledgments.</summary>
    [HttpGet("me/privacy-notice")]
    [ProducesResponseType(typeof(PatientPrivacyNoticeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyPrivacyNotice() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _privacyNoticeService.GetOwnStatusAsync(patientCode),
            errorMapper: _privacyNoticeErrorMapper,
            operationName: nameof(GetMyPrivacyNotice)
        );

    /// <summary>The authenticated patient acknowledges the notice in force. Idempotent.</summary>
    [HttpPost("me/privacy-notice/acknowledgment")]
    [ProducesResponseType(typeof(PatientPrivacyNoticeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AcknowledgeMyPrivacyNotice() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _privacyNoticeService.AcknowledgeOwnAsync(patientCode),
            errorMapper: _privacyNoticeErrorMapper,
            operationName: nameof(AcknowledgeMyPrivacyNotice)
        );

    /// <summary>Authorizations (consents) of the authenticated patient, newest first.</summary>
    [HttpGet("me/consents")]
    [ProducesResponseType(typeof(ConsentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyConsents([FromQuery] ConsentPageRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _consentService.GetOwnPageAsync(patientCode, request),
            errorMapper: _consentErrorMapper,
            operationName: nameof(GetMyConsents)
        );

    /// <summary>The authenticated patient withdraws one of their authorizations.</summary>
    [HttpPost("me/consents/{consentCode:guid}/revoke")]
    [ProducesResponseType(typeof(ConsentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RevokeMyConsent(Guid consentCode, [FromBody] RevokeConsentRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _consentService.RevokeOwnAsync(patientCode, consentCode, request),
            errorMapper: _consentErrorMapper,
            operationName: nameof(RevokeMyConsent)
        );
}
