export type ViewportSize = { width: number; height: number };

/**
 * `visualViewport` reports the *visual* viewport - what the user currently sees. On mobile it
 * diverges from the layout viewport while pinch-zoomed, while iOS auto-zooms into a focused
 * input, and while the URL bar collapses. Feeding those raw numbers into the player's sizing
 * variables makes the card jump around (and, when the value lands above the layout width, pushes
 * the card's right edge - with the AI and fullscreen buttons on it - off screen).
 *
 * Clamping each axis against the layout viewport keeps the player inside the page in every one
 * of those states while still tracking the genuinely smaller URL-bar height.
 */
export function clampVideoViewportSize(visual: ViewportSize | null, layout: ViewportSize): ViewportSize {
  const width = Math.min(visual?.width || layout.width, layout.width);
  const height = Math.min(visual?.height || layout.height, layout.height);
  return {
    width: Math.max(1, Math.round(width * 100) / 100),
    height: Math.max(1, Math.round(height * 100) / 100),
  };
}

/** Reads the current clamped viewport size from the DOM. */
export function readVideoViewportSize(): ViewportSize {
  const visualViewport = window.visualViewport;
  return clampVideoViewportSize(
    visualViewport ? { width: visualViewport.width, height: visualViewport.height } : null,
    {
      width: document.documentElement.clientWidth || window.innerWidth,
      height: document.documentElement.clientHeight || window.innerHeight,
    },
  );
}
