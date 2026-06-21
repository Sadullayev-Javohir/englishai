using Application.Competition.Dtos;
using Application.Competition.Ports;
using Application.Grammar.Ports;
using Application.Vocabulary.Ports;
using Domain.Competition;
using Domain.Grammar;
using Domain.Vocabulary;
using MediatR;

namespace Application.Competition.Create;

public sealed class CreateCompetitionCommandHandler
    : IRequestHandler<CreateCompetitionCommand, CompetitionDto>
{
    private readonly ICompetitionRepository _repo;
    private readonly IVocabularyTopicRepository _topics;
    private readonly IGrammarRepository _grammar;

    public CreateCompetitionCommandHandler(
        ICompetitionRepository repo,
        IVocabularyTopicRepository topics,
        IGrammarRepository grammar)
    {
        _repo = repo;
        _topics = topics;
        _grammar = grammar;
    }

    public async Task<CompetitionDto> Handle(CreateCompetitionCommand request, CancellationToken cancellationToken)
    {
        // Load the selected topics; only filled vocabulary topics contribute questions (domain guards).
        // Each id is either a vocabulary topic or a grammar lesson (never both), so ids left unresolved
        // by the vocabulary batch are looked up as grammar lessons in a second batch - two round trips
        // total instead of up to 2xN.
        var vocabularyTopics = (await _topics.GetByIdsAsync(request.TopicIds, cancellationToken)).ToList();
        var resolvedIds = vocabularyTopics.Select(t => t.Id).ToHashSet();
        var remainingIds = request.TopicIds.Where(id => !resolvedIds.Contains(id)).ToList();
        var grammarLessons = (await _grammar.GetByIdsAsync(remainingIds, cancellationToken)).ToList();

        var competition = Domain.Competition.Competition.Create(
            request.HostLearnerId, request.HostDisplayName, request.Title, request.Settings, request.TopicIds);

        competition.BuildSlides(vocabularyTopics, grammarLessons);

        await _repo.AddAsync(competition, cancellationToken);

        return CompetitionMapper.ToDto(competition, competition.AccessCode);
    }
}
