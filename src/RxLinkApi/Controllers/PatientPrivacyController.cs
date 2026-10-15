using Application.Core.DTOs.Consent.Errors;
using Application.Core.DTOs.Consent.Request;
using Application.Core.DTOs.Consent.Response;
using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.PrivacyNotice.Request;
using Application.Core.DTOs.PrivacyNotice.Response;
using Application.Core.Interfaces.Consent;
using Application.Core.Interfaces.PrivacyNotice;
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

/// <summary>Privacy-rights data of one patient (amendment history, accounting of disclosures), for staff.</summary>
[ApiController]
[Route("api/patient/{code:guid}")]
public sealed class PatientPrivacyController : FunctionalController
{
    private readonly IAmendment _amendments;
    private readonly IDisclosure _disclosures;
    private readonly ICommunicationPreference _preferences;
    private readonly IPrivacyNotice _privacyNotice;
    private readonly IConsent _consents;
    private readonly IErrorHttpMapper<PrivacyNoticeError> _privacyNoticeErrorMapper;
    private readonly IErrorHttpMapper<ConsentError> _consentErrorMapper;
    private readonly IErrorHttpMapper<AmendmentError> _amendmentErrorMapper;
    private readonly IErrorHttpMapper<DisclosureError> _disclosureErrorMapper;
    private readonly IErrorHttpMapper<CommunicationPreferenceError> _preferenceErrorMapper;

    public PatientPrivacyController(
        IAmendment amendments,
        IDisclosure disclosures,
        ICommunicationPreference preferences,
        IPrivacyNotice privacyNotice,
        IConsent consents,
        IErrorHttpMapper<PrivacyNoticeError> privacyNoticeErrorMapper,
        IErrorHttpMapper<ConsentError> consentErrorMapper,
        IErrorHttpMapper<AmendmentError> amendmentErrorMapper,
        IErrorHttpMapper<DisclosureError> disclosureErrorMapper,
        IErrorHttpMapper<CommunicationPreferenceError> preferenceErrorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _amendments = amendments;
        _disclosures = disclosures;
        _amendmentErrorMapper = amendmentErrorMapper;
        _disclosureErrorMapper = disclosureErrorMapper;
        _preferences = preferences;
        _privacyNotice = privacyNotice;
        _consents = consents;
        _privacyNoticeErrorMapper = privacyNoticeErrorMapper;
        _consentErrorMapper = consentErrorMapper;
        _preferenceErrorMapper = preferenceErrorMapper;
    }

    /// <summary>Amendment requests of the patient, newest first.</summary>
    [HttpGet("amendment-requests")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(AmendmentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAmendments(Guid code, [FromQuery] AmendmentPageRequest request) =>
        ExecuteAsync(
            operation: () => _amendments.GetPageByPatientAsync(code, request),
            errorMapper: _amendmentErrorMapper,
            operationName: nameof(GetAmendments)
        );

    /// <summary>Accounting of disclosures for the patient, newest first.</summary>
    [HttpGet("disclosures")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(DisclosurePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetDisclosures(Guid code, [FromQuery] DisclosurePageRequest request) =>
        ExecuteAsync(
            operation: () => _disclosures.GetPageAsync(code, request),
            errorMapper: _disclosureErrorMapper,
            operationName: nameof(GetDisclosures)
        );

    /// <summary>Records a disclosure outside treatment, payment and operations. Append-only.</summary>
    [HttpPost("disclosures")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(DisclosureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RecordDisclosure(Guid code, [FromBody] CreateDisclosureRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _disclosures.RecordAsync(code, request, userCode),
            errorMapper: _disclosureErrorMapper,
            operationName: nameof(RecordDisclosure),
            successMapper: disclosure =>
                Created($"api/patient/{code}/disclosures/{disclosure.DisclosureCode}", disclosure)
        );

    /// <summary>Channel restrictions and alternative contact of the patient (defaults when never set).</summary>
    [HttpGet("communication-preferences")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(CommunicationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetCommunicationPreferences(Guid code) =>
        ExecuteAsync(
            operation: () => _preferences.GetAsync(code),
            errorMapper: _preferenceErrorMapper,
            operationName: nameof(GetCommunicationPreferences)
        );

    /// <summary>Replaces the patient's channel restrictions and alternative contact.</summary>
    [HttpPut("communication-preferences")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(CommunicationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateCommunicationPreferences(
        Guid code, [FromBody] UpdateCommunicationPreferenceRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _preferences.UpdateAsync(code, request, userCode),
            errorMapper: _preferenceErrorMapper,
            operationName: nameof(UpdateCommunicationPreferences)
        );

    /// <summary>Notice of Privacy Practices in force and the patient's acknowledgments.</summary>
    [HttpGet("privacy-notice")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(PatientPrivacyNoticeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPrivacyNoticeStatus(Guid code) =>
        ExecuteAsync(
            operation: () => _privacyNotice.GetStatusAsync(code),
            errorMapper: _privacyNoticeErrorMapper,
            operationName: nameof(GetPrivacyNoticeStatus)
        );

    /// <summary>Records that the patient acknowledged the notice in force. Idempotent.</summary>
    [HttpPost("privacy-notice/acknowledgment")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(PatientPrivacyNoticeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AcknowledgePrivacyNotice(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _privacyNotice.AcknowledgeAsync(code, userCode),
            errorMapper: _privacyNoticeErrorMapper,
            operationName: nameof(AcknowledgePrivacyNotice)
        );

    /// <summary>Authorizations (consents) of the patient, newest first.</summary>
    [HttpGet("consents")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(ConsentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetConsents(Guid code, [FromQuery] ConsentPageRequest request) =>
        ExecuteAsync(
            operation: () => _consents.GetPageAsync(code, request),
            errorMapper: _consentErrorMapper,
            operationName: nameof(GetConsents)
        );

    /// <summary>Records an authorization granted by the patient. Never edited afterwards, only revoked.</summary>
    [HttpPost("consents")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(ConsentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RecordConsent(Guid code, [FromBody] CreateConsentRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _consents.RecordAsync(code, request, userCode),
            errorMapper: _consentErrorMapper,
            operationName: nameof(RecordConsent),
            successMapper: consent => Created($"api/patient/{code}/consents/{consent.ConsentCode}", consent)
        );

    /// <summary>Revokes an authorization. The record stays; it is marked revoked.</summary>
    [HttpPost("consents/{consentCode:guid}/revoke")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(ConsentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RevokeConsent(Guid code, Guid consentCode, [FromBody] RevokeConsentRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _consents.RevokeAsync(code, consentCode, request, userCode),
            errorMapper: _consentErrorMapper,
            operationName: nameof(RevokeConsent)
        );
}
