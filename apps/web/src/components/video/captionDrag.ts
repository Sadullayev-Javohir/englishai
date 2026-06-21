export interface Point {
  x: number;
  y: number;
}

export interface RectEdges {
  left: number;
  right: number;
  top: number;
  bottom: number;
}

export function clampCaptionOffset(
  desired: Point,
  current: Point,
  frame: RectEdges,
  caption: RectEdges,
): Point {
  const baseLeft = caption.left - current.x;
  const baseRight = caption.right - current.x;
  const baseTop = caption.top - current.y;
  const baseBottom = caption.bottom - current.y;

  return {
    x: Math.min(Math.max(desired.x, frame.left - baseLeft), frame.right - baseRight),
    y: Math.min(Math.max(desired.y, frame.top - baseTop), frame.bottom - baseBottom),
  };
}
