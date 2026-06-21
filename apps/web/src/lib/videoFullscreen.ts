export const RESPONSIVE_VIDEO_MAX_WIDTH = 1180;

export function shouldRotateResponsiveFullscreen(
  fullscreen: boolean,
  viewportWidth: number,
  viewportHeight: number,
): boolean {
  return fullscreen
    && viewportWidth <= RESPONSIVE_VIDEO_MAX_WIDTH
    && viewportHeight > viewportWidth;
}
