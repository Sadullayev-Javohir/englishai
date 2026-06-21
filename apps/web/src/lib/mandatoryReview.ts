export const MANDATORY_REVIEW_REQUIRED_EVENT = "review:required";
export const MANDATORY_REVIEW_COMPLETED_EVENT = "review:completed";

export function notifyMandatoryReviewCompleted(): void {
  window.dispatchEvent(new CustomEvent(MANDATORY_REVIEW_COMPLETED_EVENT));
}
