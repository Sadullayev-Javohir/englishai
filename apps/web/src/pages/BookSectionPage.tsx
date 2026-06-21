import { Navigate, useParams } from "react-router-dom";

/** Legacy deep links remain valid, but the canonical book route owns the continuous runner. */
export function BookSectionPage() {
  const { bookId = "", sectionId = "" } = useParams();
  const query = new URLSearchParams();
  if (sectionId) query.set("section", sectionId);

  return <Navigate to={`/books/${bookId}${query.size ? `?${query.toString()}` : ""}`} replace />;
}
