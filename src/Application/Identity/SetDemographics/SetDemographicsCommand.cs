using Application.Identity.Dtos;
using Domain.Identity;
using MediatR;

namespace Application.Identity.SetDemographics;

public sealed record SetDemographicsCommand(
    Guid UserId,
    DateOnly BirthDate,
    Gender Gender,
    AcquisitionSource AcquisitionSource,
    string? AcquisitionSourceOther) : IRequest<AuthenticatedUserDto>;
