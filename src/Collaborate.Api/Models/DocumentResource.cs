namespace Collaborate.Api.Models;

public sealed record DocumentResource(
    string DocumentId,
    string WorkspaceId,
    string FirmId,
    string Name);
