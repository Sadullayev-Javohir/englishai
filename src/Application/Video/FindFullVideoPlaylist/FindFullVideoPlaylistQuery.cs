using Application.Video.Dtos;
using MediatR;

namespace Application.Video.FindFullVideoPlaylist;

public sealed record FindFullVideoPlaylistQuery(string Query) : IRequest<VideoPlaylistSearchDto>;
public sealed record GetFullVideoPlaylistQuery(string PlaylistId) : IRequest<VideoPlaylistDto?>;
public sealed record GetFeaturedVideoPlaylistQuery() : IRequest<VideoPlaylistDto?>;
