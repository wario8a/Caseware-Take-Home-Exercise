using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Collaborate.Api.Tests;

public sealed class DocumentsAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Issuer = "https://auth.caseware.local";
    private const string Audience = "collaborate-api";
    private const string SigningKey = "caseware-local-test-signing-key-1234567890";
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentsAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDocument_ReturnsUnauthorized_WhenTokenIsMissing()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsOk_WhenTokenIsValidAndAuthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-001", "documents.read"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsForbidden_WhenScopeIsMissing()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-001", "comments.read"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsForbidden_WhenTokenOnlyHasDocumentsWrite()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-001", "documents.write"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsForbidden_WhenFirmDoesNotMatch()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-999", "documents.read"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsForbidden_WhenFirmIdIsMissing()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(null, "documents.read"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsNotFound_WhenAuthorizedResourceDoesNotExist()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-001", "documents.read"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/missing-doc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsUnauthorized_WhenTokenIsExpired()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-001", "documents.read", expiresAtUtc: DateTime.UtcNow.AddMinutes(-5)));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_ReturnsUnauthorized_WhenSignatureIsInvalid()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("firm-001", "documents.read", signingKey: "invalid-signing-key-123456789012345"));

        var response = await client.GetAsync("/api/workspaces/ws-123/documents/doc-456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateToken(
        string? firmId,
        string scope,
        DateTime? expiresAtUtc = null,
        string? signingKey = null)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "user-123"),
            new("scope", scope)
        };
        if (!string.IsNullOrWhiteSpace(firmId))
        {
            claims.Add(new Claim("firm_id", firmId));
        }
        var expires = expiresAtUtc ?? DateTime.UtcNow.AddMinutes(30);
        var notBefore = expires <= DateTime.UtcNow
            ? expires.AddMinutes(-10)
            : DateTime.UtcNow.AddMinutes(-1);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
