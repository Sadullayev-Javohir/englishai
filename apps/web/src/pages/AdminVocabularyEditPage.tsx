import { useParams } from "react-router-dom";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { useDocumentTitle } from "@/app/documentTitle";
import { AppButton, ErrorState, PageHeader } from "@/components/design";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { VocabularyForm, rowToForm } from "./AdminVocabularyPage";

export function AdminVocabularyEditPage() {
  const { id } = useParams<{ id: string }>();
  const { data, loading, error, reload } = useAsync(
    () => id ? api.admin.vocabulary.get(id) : Promise.reject(new Error("Missing vocabulary topic id")),
    [id],
    Boolean(id),
  );
  useDocumentTitle(data?.title, "Vocabulary tahrirlash");

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Admin workspace"
        title="Vocabulary mavzusini tahrirlash"
        description="PostgreSQL passage va barcha word contextlarini boshqarish"
        actions={<AppButton tone="standard" leadingIcon="close" onClick={() => window.close()}>Tabni yopish</AppButton>}
      />

      {loading ? <LoadingSkeleton variant="form" /> : null}
      {error || (!loading && !data) ? (
        <ErrorState
          title="Mavzuni yuklab bo‘lmadi"
          action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton>}
        />
      ) : null}
      {data ? (
        <VocabularyForm
          key={data.id}
          editing={{ id: data.id, form: rowToForm(data) }}
          onSaved={() => { window.opener?.location.reload(); window.close(); }}
          onClose={() => window.close()}
        />
      ) : null}
    </main>
  );
}
