using Collaborate.Api.Common;
using Collaborate.Api.Models;

namespace Collaborate.Api.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly ILogger<DocumentService> _logger;
    private readonly IReadOnlyDictionary<string, DocumentResource> _documents;

    public DocumentService(ILogger<DocumentService> logger)
    {
        _logger = logger;
        _documents = new Dictionary<string, DocumentResource>(StringComparer.OrdinalIgnoreCase)
        {
            ["ws-123:doc-456"] = new("doc-456", "ws-123", "firm-001", "Quarterly Financial Summary"),
            ["ws-456:doc-789"] = new("doc-789", "ws-456", "firm-002", "Audit Notes")
        };
    }

    public async Task<Result<DocumentResource>> GetAsync(
        string workspaceId,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(workspaceId, documentId);
        if (_documents.TryGetValue(key, out var document))
        {
            _logger.LogInformation(
                "Resolved document {DocumentId} for workspace {WorkspaceId}.",
                documentId,
                workspaceId);

            return Result<DocumentResource>.Success(document);
        }

        _logger.LogInformation(
            "Document {DocumentId} was not found in workspace {WorkspaceId}.",
            documentId,
            workspaceId);

        return
            Result<DocumentResource>.NotFound(
                $"Document '{documentId}' was not found in workspace '{workspaceId}'.");
    }

    private static string BuildKey(string workspaceId, string documentId) =>
        $"{workspaceId}:{documentId}";
}
