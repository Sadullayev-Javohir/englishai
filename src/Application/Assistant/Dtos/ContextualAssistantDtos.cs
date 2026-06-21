namespace Application.Assistant.Dtos;

public sealed record ContextualAssistantTurnDto(string Role, string Text);
public sealed record ContextualAssistantReplyDto(string? ReplyUz);
