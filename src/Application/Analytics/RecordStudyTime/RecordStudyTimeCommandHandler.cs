using Application.Analytics.Ports;
using MediatR;

namespace Application.Analytics.RecordStudyTime;

public sealed class RecordStudyTimeCommandHandler : IRequestHandler<RecordStudyTimeCommand>
{
    private readonly IStudyLogStore _store;

    public RecordStudyTimeCommandHandler(IStudyLogStore store)
    {
        _store = store;
    }

    public async Task Handle(RecordStudyTimeCommand request, CancellationToken cancellationToken) =>
        await _store.AddStudyTimeAsync(
            request.LearnerId, request.LocalDate, request.Skill, request.Seconds, cancellationToken);
}
