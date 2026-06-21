using MediatR;
using Application.Identity.AuthenticateWithGoogle;

namespace Application.Identity.DevLogin;

/// <summary>
/// Development-only sign-in bypass. When the backend runs with the Development environment
/// (local `dotnet run`), the SPA shows a "Dev kirish" button that hits this endpoint instead of
/// the Google OAuth round-trip, so the app can be exercised without a Google Cloud project.
/// The endpoint is NOT registered in Production (see Web/Endpoints/AuthEndpoints.cs), and the
/// SPA only reveals the button under Vite's dev build, so this never ships as a real entry point.
/// </summary>
public sealed record DevLoginCommand : IRequest<AuthenticateWithGoogleResult>;
