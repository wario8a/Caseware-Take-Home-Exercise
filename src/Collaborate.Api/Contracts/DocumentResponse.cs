namespace Collaborate.Api.Contracts;

public sealed record DocumentResponse(
    string DocumentId,
    string WorkspaceId,
    string FirmId,
    string Name);
