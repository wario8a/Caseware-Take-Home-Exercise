using Collaborate.Api.Common;
using Collaborate.Api.Models;

namespace Collaborate.Api.Services;

public interface IDocumentService
{
    Task<Result<DocumentResource>> GetAsync(
        string workspaceId,
        string documentId,
        CancellationToken cancellationToken = default);
}
