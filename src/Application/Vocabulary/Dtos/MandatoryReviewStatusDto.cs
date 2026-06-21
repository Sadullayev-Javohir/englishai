namespace Application.Vocabulary.Dtos;

public sealed record MandatoryReviewStatusDto(
    bool IsRequired,
    int DueItemCount,
    int DueTopicCount);
