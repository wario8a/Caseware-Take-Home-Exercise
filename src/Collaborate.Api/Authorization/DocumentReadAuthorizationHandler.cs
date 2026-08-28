using System.Security.Claims;
using Collaborate.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Collaborate.Api.Authorization;

public sealed class DocumentReadAccessContextAuthorizationHandler
    : AuthorizationHandler<DocumentReadRequirement, DocumentAccessContext>
{
    private readonly ILogger<DocumentReadAccessContextAuthorizationHandler> _logger;

    public DocumentReadAccessContextAuthorizationHandler(
        ILogger<DocumentReadAccessContextAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentReadRequirement requirement,
        DocumentAccessContext resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var firmId = context.User.FindFirstValue("firm_id");
        if (string.IsNullOrWhiteSpace(firmId))
        {
            _logger.LogWarning(
                "Document read denied for document {DocumentId} in workspace {WorkspaceId} because the token is missing firm_id.",
                resource.DocumentId,
                resource.WorkspaceId);
            return Task.CompletedTask;
        }

        if (!HasScope(context.User, "documents.read"))
        {
            _logger.LogWarning(
                "Document read denied for document {DocumentId} in workspace {WorkspaceId} because the token is missing documents.read.",
                resource.DocumentId,
                resource.WorkspaceId);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static bool HasScope(ClaimsPrincipal user, string requiredScope)
    {
        var scopeValue = user.FindFirstValue("scope");
        if (string.IsNullOrWhiteSpace(scopeValue))
        {
            return false;
        }

        return scopeValue
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredScope, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class DocumentReadResourceAuthorizationHandler
    : AuthorizationHandler<DocumentReadRequirement, DocumentResource>
{
    private readonly ILogger<DocumentReadResourceAuthorizationHandler> _logger;

    public DocumentReadResourceAuthorizationHandler(
        ILogger<DocumentReadResourceAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentReadRequirement requirement,
        DocumentResource resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var firmId = context.User.FindFirstValue("firm_id");
        if (string.IsNullOrWhiteSpace(firmId))
        {
            _logger.LogWarning(
                "Document read denied for document {DocumentId} because the token is missing firm_id.",
                resource.DocumentId);
            return Task.CompletedTask;
        }

        if (!string.Equals(firmId, resource.FirmId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Document read denied for document {DocumentId} because the token firm {TokenFirmId} does not match the resource firm.",
                resource.DocumentId,
                firmId);
            return Task.CompletedTask;
        }

        if (!HasScope(context.User, "documents.read"))
        {
            _logger.LogWarning(
                "Document read denied for document {DocumentId} because the token is missing documents.read.",
                resource.DocumentId);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static bool HasScope(ClaimsPrincipal user, string requiredScope)
    {
        var scopeValue = user.FindFirstValue("scope");
        if (string.IsNullOrWhiteSpace(scopeValue))
        {
            return false;
        }

        return scopeValue
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredScope, StringComparer.OrdinalIgnoreCase);
    }
}
