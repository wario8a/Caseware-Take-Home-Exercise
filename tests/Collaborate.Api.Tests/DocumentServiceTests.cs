using Collaborate.Api.Common;
using Collaborate.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Collaborate.Api.Tests;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsSuccess_WhenDocumentExists()
    {
        var service = new DocumentService(new NullLogger<DocumentService>());

        var result = await service.GetAsync("ws-123", "doc-456");

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("firm-001", result.Value!.FirmId);
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        var service = new DocumentService(new NullLogger<DocumentService>());

        var result = await service.GetAsync("ws-123", "missing-doc");

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
