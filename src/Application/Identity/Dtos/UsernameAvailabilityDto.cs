namespace Application.Identity.Dtos;

/// <summary>
/// The result of a live username check. <see cref="IsValidFormat"/> reports whether the value
/// satisfies the format rules at all; <see cref="IsAvailable"/> whether it is free to claim
/// (true only when the format is valid and no other account holds it).
/// </summary>
public sealed record UsernameAvailabilityDto(bool IsValidFormat, bool IsAvailable);
