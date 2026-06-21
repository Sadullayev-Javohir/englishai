using Application.Competition.Advance;
using Application.Competition;
using Application.Competition.Dtos;
using Application.Competition.Finish;
using Application.Competition.Join;
using Application.Competition.Ports;
using Application.Competition.Start;
using Application.Competition.SubmitAnswer;
using Application.Common;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Web.Observability;

namespace Web.Hubs;

/// <summary>
/// Real-time channel for the live "Competition" ("Musobaqa") game. Clients invoke the
/// server methods below (join / start / answer / advance / finish / leave); the hub stays
/// thin and delegates every mutation to a MediatR command, then broadcasts the resulting
/// domain view to all authenticated connections in the competition's SignalR group.
/// </summary>
public sealed class CompetitionHub : Hub
{
    /// <summary>Group name prefix for a competition's SignalR group.</summary>
    public const string GroupPrefix = "competition";

    /// <summary>A participant joined the lobby - carries the joined <see cref="ParticipantDto"/>.</summary>
    public const string ParticipantJoinedEvent = "ParticipantJoined";

    /// <summary>The host started the game - carries the full <see cref="CompetitionDto"/>.</summary>
    public const string CompetitionStartedEvent = "CompetitionStarted";

    /// <summary>The host advanced to the next slide - carries the full <see cref="CompetitionDto"/>.</summary>
    public const string SlideAdvancedEvent = "SlideAdvanced";

    /// <summary>Countdown tick for the current slide - carries the remaining-time payload.</summary>
    public const string TickEvent = "Tick";

    /// <summary>Scores changed (after an answer) - carries the updated participant list.</summary>
    public const string ScoresUpdatedEvent = "ScoresUpdated";

    /// <summary>The game ended - carries the final <see cref="CompetitionResultDto"/>.</summary>
    public const string CompetitionFinishedEvent = "CompetitionFinished";

    private readonly ISender _sender;
    private readonly ILogger<CompetitionHub> _logger;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICompetitionRepository _competitions;

    public CompetitionHub(
        ISender sender,
        ILogger<CompetitionHub> logger,
        ICurrentUserAccessor currentUser,
        ICompetitionRepository competitions)
    {
        _sender = sender;
        _logger = logger;
        _currentUser = currentUser;
        _competitions = competitions;
    }

    /// <summary>SignalR group name for a competition.</summary>
    private static string GroupName(Guid id) => $"{GroupPrefix}:{id}";

    /// <summary>
    /// Join (or create-and-join) a competition. Adds the connection to the competition group
    /// and broadcasts the joined participant to the rest of the group.
    /// </summary>
    public async Task JoinCompetition(
        Guid competitionId,
        string displayName,
        string? accessCode,
        CancellationToken cancellationToken = default)
    {
        var authenticatedLearnerId = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var dto = await _sender.Send(
            new Application.Competition.Join.JoinCompetitionCommand(
                competitionId, authenticatedLearnerId, displayName, accessCode),
            cancellationToken);

        await AddToCompetitionGroupAsync(competitionId, "joined", cancellationToken);

        var joined = dto.Participants.LastOrDefault(p => p.LearnerId == authenticatedLearnerId)
                     ?? dto.Participants.LastOrDefault();

        await SendToGroupAsync(
            competitionId,
            ParticipantJoinedEvent,
            joined,
            cancellationToken);

        _logger.LogInformation(
            "Learner {LearnerId} joined competition {CompetitionId} (conn {ConnectionId}).",
            authenticatedLearnerId, competitionId, Context.ConnectionId);
    }

    /// <summary>Host starts the competition - broadcasts the started view to the group.</summary>
    public async Task StartCompetition(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var authenticatedLearnerId = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var dto = await _sender.Send(
            new Application.Competition.Start.StartCompetitionCommand(competitionId, authenticatedLearnerId),
            cancellationToken);

        await SendToGroupAsync(
            competitionId,
            CompetitionStartedEvent,
            dto,
            cancellationToken);
    }

    /// <summary>
    /// A participant submits an answer for the current slide. Broadcasts the updated scores
    /// (the participant list) to the group.
    /// </summary>
    public async Task SubmitAnswer(
        Guid competitionId,
        int selectedOptionIndex,
        double timeRatioRemaining = 1.0,
        CancellationToken cancellationToken = default)
    {
        var authenticatedLearnerId = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var participant = await ResolveParticipantAsync(competitionId, authenticatedLearnerId, cancellationToken);
        var dto = await _sender.Send(
            new SubmitSlideAnswerCommand(
                competitionId, participant, selectedOptionIndex, timeRatioRemaining),
            cancellationToken);

        await SendToGroupAsync(
            competitionId,
            ScoresUpdatedEvent,
            dto.Participants,
            cancellationToken);
    }

    /// <summary>Host advances to the next slide - broadcasts the advanced view to the group.</summary>
    public async Task AdvanceSlide(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var authenticatedLearnerId = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var dto = await _sender.Send(
            new AdvanceSlideCommand(competitionId, authenticatedLearnerId),
            cancellationToken);

        await SendToGroupAsync(
            competitionId,
            SlideAdvancedEvent,
            dto,
            cancellationToken);
    }

    /// <summary>Host finishes the game - broadcasts the final results to the group.</summary>
    public async Task FinishCompetition(Guid competitionId, CancellationToken cancellationToken = default)
    {
        var authenticatedLearnerId = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var result = await _sender.Send(
            new FinishCompetitionCommand(competitionId, authenticatedLearnerId),
            cancellationToken);

        await SendToGroupAsync(
            competitionId,
            CompetitionFinishedEvent,
            result,
            cancellationToken);
    }

    /// <summary>
    /// Restores group membership after SignalR reconnect. Only an authenticated learner already
    /// persisted as a participant can resume; presenting another learner's id is impossible because
    /// the caller identity comes exclusively from the validated JWT.
    /// </summary>
    public async Task<CompetitionDto> ResumeCompetition(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var authenticatedLearnerId = ResourceOwnership.RequireCurrentLearner(_currentUser);
        var competition = await _competitions.GetByIdAsync(competitionId, cancellationToken)
                          ?? throw new Domain.Common.DomainException("Musobaqa topilmadi.");

        if (!competition.Participants.Any(participant => participant.LearnerId == authenticatedLearnerId))
        {
            EnglishAiTelemetry.SignalRGroupEvents.Add(
                1,
                new("hub", "competition"),
                new("operation", "resume"),
                new("outcome", "forbidden"));
            throw new ForbiddenException("You can only resume a competition you joined.");
        }

        await AddToCompetitionGroupAsync(competitionId, "resumed", cancellationToken);
        return CompetitionMapper.ToDto(competition);
    }

    /// <summary>Leave the competition group (called on disconnect / explicit leave).</summary>
    public async Task LeaveCompetition(Guid competitionId, CancellationToken cancellationToken = default)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(competitionId), cancellationToken);
        EnglishAiTelemetry.SignalRGroupEvents.Add(
            1,
            new("hub", "competition"),
            new("operation", "leave"),
            new("outcome", "removed"));
    }

    public override async Task OnConnectedAsync()
    {
        EnglishAiTelemetry.SignalRConnections.Add(1, new KeyValuePair<string, object?>("hub", "competition"));
        EnglishAiTelemetry.SignalRConnectionEvents.Add(1, new("hub", "competition"), new("outcome", "connected"));
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        EnglishAiTelemetry.SignalRConnections.Add(-1, new KeyValuePair<string, object?>("hub", "competition"));
        EnglishAiTelemetry.SignalRConnectionEvents.Add(
            1,
            new("hub", "competition"),
            new("outcome", exception is null ? "disconnected" : "failed"));
        // SignalR removes group membership automatically on disconnect; nothing else required
        // here because the domain tracks participation via commands, not connections.
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<Guid> ResolveParticipantAsync(
        Guid competitionId, Guid learnerId, CancellationToken cancellationToken)
    {
        var competition = await _competitions.GetByIdAsync(competitionId, cancellationToken)
                          ?? throw new Domain.Common.DomainException("Musobaqa topilmadi.");
        var participant = competition.Participants.SingleOrDefault(item => item.LearnerId == learnerId);
        if (participant is null)
            throw new ForbiddenException("You can only answer as your own participant.");

        return participant.Id;
    }

    private async Task AddToCompetitionGroupAsync(
        Guid competitionId,
        string operation,
        CancellationToken cancellationToken)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(competitionId), cancellationToken);
        EnglishAiTelemetry.SignalRGroupEvents.Add(
            1,
            new("hub", "competition"),
            new("operation", operation),
            new("outcome", "added"));
    }

    private async Task SendToGroupAsync<T>(
        Guid competitionId,
        string eventName,
        T payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await Clients.Group(GroupName(competitionId))
                .SendAsync(eventName, payload, cancellationToken);
            EnglishAiTelemetry.SignalRSendEvents.Add(
                1,
                new("hub", "competition"),
                new("target", "group"),
                new("outcome", "sent"));
        }
        catch
        {
            EnglishAiTelemetry.SignalRSendEvents.Add(
                1,
                new("hub", "competition"),
                new("target", "group"),
                new("outcome", "failed"));
            throw;
        }
    }
}
