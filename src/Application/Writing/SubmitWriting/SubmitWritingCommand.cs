using Application.Writing.Dtos;
using MediatR;

namespace Application.Writing.SubmitWriting;

/// <summary>
/// Submits a learner's written text for AI assessment (PROJECT-SPEC G.3). The task is keyed by its
/// learning-spine topic (every skill teaches the same topics). The assessment is a gated premium
/// feature (Free: 3/month, H.1); on success the Writing skill score is recorded (G.4), any grammar
/// issues feed the error heatmap (C.7), and the overall score is credited toward the topic's
/// Writing module (K.5).
/// </summary>
public sealed record SubmitWritingCommand(
    Guid TopicId,
    Guid LearnerId,
    string Text) : IRequest<WritingAssessmentDto>;
