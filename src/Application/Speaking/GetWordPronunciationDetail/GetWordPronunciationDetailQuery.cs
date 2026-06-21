using Application.Speaking.Dtos;
using MediatR;

namespace Application.Speaking.GetWordPronunciationDetail;

/// <summary>
/// Powers the pronunciation detail screen: returns a word's IPA transcription, the
/// SVG mouth shapes for each phoneme, and an Uzbek tip for the hardest sound.
/// </summary>
public sealed record GetWordPronunciationDetailQuery(string Word)
    : IRequest<WordPronunciationDetailDto>;
