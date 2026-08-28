using System.Security.Claims;
using Collaborate.Api.Authorization;
using Collaborate.Api.Common;
using Collaborate.Api.Controllers;
using Collaborate.Api.Contracts;
using Collaborate.Api.Models;
using Collaborate.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Collaborate.Api.Tests;

public sealed class DocumentsControllerTests
{
    [Fact]
    public async Task Get_ReturnsForbid_WhenCoarseAuthorizationFails()
    {
        var controller = CreateController(
            authorizationService: new FakeAuthorizationService(
                accessContextResult: AuthorizationResult.Failed(),
                resourceResult: AuthorizationResult.Success()),
            documentService: new FakeDocumentService(
                Result<DocumentResource>.Success(new DocumentResource("doc-456", "ws-123", "firm-001", "Quarterly Financial Summary"))));

        var result = await controller.Get("ws-123", "doc-456", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenAuthorizedDocumentDoesNotExist()
    {
        var controller = CreateController(
            authorizationService: new FakeAuthorizationService(
                accessContextResult: AuthorizationResult.Success(),
                resourceResult: AuthorizationResult.Success()),
            documentService: new FakeDocumentService(Result<DocumentResource>.NotFound()));

        var result = await controller.Get("ws-123", "missing-doc", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsForbid_WhenResourceAuthorizationFails()
    {
        var controller = CreateController(
            authorizationService: new FakeAuthorizationService(
                accessContextResult: AuthorizationResult.Success(),
                resourceResult: AuthorizationResult.Failed()),
            documentService: new FakeDocumentService(
                Result<DocumentResource>.Success(new DocumentResource("doc-456", "ws-123", "firm-001", "Quarterly Financial Summary"))));

        var result = await controller.Get("ws-123", "doc-456", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenAuthorizedDocumentExists()
    {
        var document = new DocumentResource("doc-456", "ws-123", "firm-001", "Quarterly Financial Summary");
        var controller = CreateController(
            authorizationService: new FakeAuthorizationService(
                accessContextResult: AuthorizationResult.Success(),
                resourceResult: AuthorizationResult.Success()),
            documentService: new FakeDocumentService(Result<DocumentResource>.Success(document)));

        var result = await controller.Get("ws-123", "doc-456", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DocumentResponse>(okResult.Value);
        Assert.Equal(document.DocumentId, response.DocumentId);
        Assert.Equal(document.WorkspaceId, response.WorkspaceId);
        Assert.Equal(document.FirmId, response.FirmId);
        Assert.Equal(document.Name, response.Name);
    }

    private static DocumentsController CreateController(
        IAuthorizationService authorizationService,
        IDocumentService documentService)
    {
        var controller = new DocumentsController(
            authorizationService,
            documentService,
            new NullLogger<DocumentsController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = BuildPrincipal()
                }
            }
        };

        return controller;
    }

    private static ClaimsPrincipal BuildPrincipal()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim("firm_id", "firm-001"),
            new Claim("scope", "documents.read")
        ], "Test");

        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeAuthorizationService : IAuthorizationService
    {
        private readonly AuthorizationResult _accessContextResult;
        private readonly AuthorizationResult _resourceResult;

        public FakeAuthorizationService(
            AuthorizationResult accessContextResult,
            AuthorizationResult resourceResult)
        {
            _accessContextResult = accessContextResult;
            _resourceResult = resourceResult;
        }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            return Task.FromResult(resource switch
            {
                DocumentAccessContext => _accessContextResult,
                DocumentResource => _resourceResult,
                _ => AuthorizationResult.Failed()
            });
        }

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            return Task.FromResult(resource switch
            {
                DocumentAccessContext => _accessContextResult,
                DocumentResource => _resourceResult,
                _ => AuthorizationResult.Failed()
            });
        }
    }

    private sealed class FakeDocumentService : IDocumentService
    {
        private readonly Result<DocumentResource> _result;

        public FakeDocumentService(Result<DocumentResource> result)
        {
            _result = result;
        }

        public Task<Result<DocumentResource>> GetAsync(
            string workspaceId,
            string documentId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_result);
        }
    }
}
