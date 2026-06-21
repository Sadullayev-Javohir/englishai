using Application.Vocabulary.Dtos;
using Domain.Listening;

namespace Application.Listening.Admin;

public sealed record ListeningQuestionAdminDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    string? HintCode,
    string? Explanation);

public sealed record ListeningAudioAdminDto(
    string StreamUrl,
    bool HasCachedAudio,
    long? SizeBytes,
    DateTimeOffset? GeneratedAt,
    string ContentType);

public sealed record ListeningSegmentAdminDto(
    Guid Id,
    int Order,
    int StartMs,
    int EndMs,
    string Speaker,
    string Text);

public sealed record ListeningExerciseAdminDetailDto(
    Guid Id,
    string Title,
    string Topic,
    string Level,
    string Status,
    Guid? VocabularyTopicId,
    string Transcript,
    ListeningAudioAdminDto Audio,
    IReadOnlyList<ListeningSegmentAdminDto> Segments,
    IReadOnlyList<ListeningQuestionAdminDto> Questions,
    IReadOnlyList<TargetWordDto> VocabularyContext,
    DateTimeOffset CreatedAt);

public sealed record ListeningQuestionAdminUpdateDto(
    Guid? Id,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    string? HintCode,
    string? Explanation);

public sealed record ListeningSegmentAdminUpdateDto(
    Guid? Id,
    int Order,
    int StartMs,
    int EndMs,
    string Speaker,
    string Text);

public sealed record ListeningExerciseAdminFullUpdateDto(
    string Title,
    string Topic,
    string Level,
    string Status,
    Guid? VocabularyTopicId,
    string Transcript,
    IReadOnlyList<ListeningSegmentAdminUpdateDto> Segments,
    IReadOnlyList<ListeningQuestionAdminUpdateDto> Questions);
