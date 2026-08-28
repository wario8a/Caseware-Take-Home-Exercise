using System.Security.Claims;
using Collaborate.Api.Authorization;
using Collaborate.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Collaborate.Api.Tests;

public sealed class AuthorizationHandlersTests
{
    [Fact]
    public async Task AccessContextHandler_Succeeds_WhenScopeAndFirmClaimArePresent()
    {
        var handler = new DocumentReadAccessContextAuthorizationHandler(
            new NullLogger<DocumentReadAccessContextAuthorizationHandler>());
        var requirement = new DocumentReadRequirement();
        var user = BuildPrincipal("firm-001", "documents.read comments.read");
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            new DocumentAccessContext("ws-123", "doc-456"));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task AccessContextHandler_Fails_WhenScopeIsMissing()
    {
        var handler = new DocumentReadAccessContextAuthorizationHandler(
            new NullLogger<DocumentReadAccessContextAuthorizationHandler>());
        var requirement = new DocumentReadRequirement();
        var user = BuildPrincipal("firm-001", "comments.read");
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            new DocumentAccessContext("ws-123", "doc-456"));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ResourceHandler_Succeeds_WhenFirmMatchesAndScopeExists()
    {
        var handler = new DocumentReadResourceAuthorizationHandler(
            new NullLogger<DocumentReadResourceAuthorizationHandler>());
        var requirement = new DocumentReadRequirement();
        var user = BuildPrincipal("firm-001", "documents.read");
        var document = new DocumentResource("doc-456", "ws-123", "firm-001", "Quarterly Financial Summary");
        var context = new AuthorizationHandlerContext([requirement], user, document);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ResourceHandler_Fails_WhenFirmDoesNotMatch()
    {
        var handler = new DocumentReadResourceAuthorizationHandler(
            new NullLogger<DocumentReadResourceAuthorizationHandler>());
        var requirement = new DocumentReadRequirement();
        var user = BuildPrincipal("firm-002", "documents.read");
        var document = new DocumentResource("doc-456", "ws-123", "firm-001", "Quarterly Financial Summary");
        var context = new AuthorizationHandlerContext([requirement], user, document);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal BuildPrincipal(string? firmId, string? scope)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(firmId))
        {
            claims.Add(new Claim("firm_id", firmId));
        }

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        claims.Add(new Claim(ClaimTypes.NameIdentifier, "user-123"));
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}
