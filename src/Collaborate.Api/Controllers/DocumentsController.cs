using System.Diagnostics;
using Collaborate.Api.Authorization;
using Collaborate.Api.Common;
using Collaborate.Api.Contracts;
using Collaborate.Api.Models;
using Collaborate.Api.Observability;
using Collaborate.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collaborate.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId}/documents")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IAuthorizationService authorizationService,
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _authorizationService = authorizationService;
        _documentService = documentService;
        _logger = logger;
    }

    [HttpGet("{documentId}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        string workspaceId,
        string documentId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = CollaborateObservability.ActivitySource.StartActivity("documents.get", ActivityKind.Internal);
        activity?.SetTag("workspace.id", workspaceId);
        activity?.SetTag("document.id", documentId);

        var tags = new KeyValuePair<string, object?>[]
        {
            new("workspace.id", workspaceId),
            new("document.id", documentId)
        };

        try
        {
            var accessContext = new DocumentAccessContext(workspaceId, documentId);
            var accessResult = await _authorizationService.AuthorizeAsync(
                User,
                accessContext,
                AuthorizationPolicies.DocumentRead);

            if (!accessResult.Succeeded)
            {
                activity?.SetTag("authorization.result", "forbidden");
                CollaborateObservability.DocumentReadRequests.Add(1, Append(tags, new("http.status_code", StatusCodes.Status403Forbidden)));
                _logger.LogWarning(
                    "Document read request for {WorkspaceId}/{DocumentId} was denied during coarse authorization.",
                    workspaceId,
                    documentId);
                return Forbid();
            }

            var documentResult = await _documentService.GetAsync(workspaceId, documentId, cancellationToken);
            if (documentResult.Status == ResultStatus.NotFound)
            {
                activity?.SetTag("resource.result", "not_found");
                CollaborateObservability.DocumentReadRequests.Add(1, Append(tags, new("http.status_code", StatusCodes.Status404NotFound)));
                _logger.LogInformation(
                    "Document {DocumentId} in workspace {WorkspaceId} was not found after authorization succeeded.",
                    documentId,
                    workspaceId);
                return NotFound();
            }

            if (!documentResult.IsSuccess || documentResult.Value is null)
            {
                activity?.SetTag("resource.result", "failure");
                CollaborateObservability.DocumentReadFailures.Add(1, Append(tags, new("failure.reason", "document_service")));
                _logger.LogError(
                    "Document service returned an unexpected failure for {WorkspaceId}/{DocumentId}: {Error}",
                    workspaceId,
                    documentId,
                    documentResult.Error);
                return Problem(statusCode: StatusCodes.Status500InternalServerError);
            }

            var document = documentResult.Value;
            var documentAuthorizationResult = await _authorizationService.AuthorizeAsync(
                User,
                document,
                AuthorizationPolicies.DocumentRead);

            if (!documentAuthorizationResult.Succeeded)
            {
                activity?.SetTag("authorization.result", "forbidden_resource");
                CollaborateObservability.DocumentReadRequests.Add(1, Append(tags, new("http.status_code", StatusCodes.Status403Forbidden)));
                _logger.LogWarning(
                    "Document read request for {WorkspaceId}/{DocumentId} was denied during resource authorization.",
                    workspaceId,
                    documentId);
                return Forbid();
            }

            if (!string.Equals(document.WorkspaceId, workspaceId, StringComparison.Ordinal))
            {
                activity?.SetTag("resource.result", "workspace_mismatch");
                CollaborateObservability.DocumentReadFailures.Add(1, Append(tags, new("failure.reason", "workspace_mismatch")));
                _logger.LogWarning(
                    "Document {DocumentId} resolved with workspace {ResolvedWorkspaceId}, which does not match the route workspace {WorkspaceId}.",
                    documentId,
                    document.WorkspaceId,
                    workspaceId);
                return Forbid();
            }

            var response = Map(document);
            activity?.SetTag("authorization.result", "success");
            CollaborateObservability.DocumentReadRequests.Add(1, Append(tags, new("http.status_code", StatusCodes.Status200OK)));
            _logger.LogInformation(
                "Document {DocumentId} in workspace {WorkspaceId} was returned successfully.",
                documentId,
                workspaceId);

            return Ok(response);
        }
        finally
        {
            stopwatch.Stop();
            CollaborateObservability.DocumentReadDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                tags);
        }
    }

    private static DocumentResponse Map(DocumentResource document) =>
        new(document.DocumentId, document.WorkspaceId, document.FirmId, document.Name);

    private static KeyValuePair<string, object?>[] Append(
        IReadOnlyList<KeyValuePair<string, object?>> tags,
        KeyValuePair<string, object?> extra)
    {
        var combined = new KeyValuePair<string, object?>[tags.Count + 1];
        for (var i = 0; i < tags.Count; i++)
        {
            combined[i] = tags[i];
        }

        combined[^1] = extra;
        return combined;
    }
}
