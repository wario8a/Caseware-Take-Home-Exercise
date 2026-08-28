namespace Collaborate.Api.Authorization;

public sealed record DocumentAccessContext(
    string WorkspaceId,
    string DocumentId);
