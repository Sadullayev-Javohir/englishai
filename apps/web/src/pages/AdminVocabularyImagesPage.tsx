import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, apiErrorDetails } from "@/api/client";
import type { AdminVocabularyImageDto } from "@/api/types";
import { AppButton, DesignInput, DesignToast, ErrorState, PageHeader, StatCard } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { useAsync } from "@/lib/useAsync";
import "./AdminVocabularyImagesPage.css";

type ToastState = { tone: "success" | "danger"; message: string } | null;

export function AdminVocabularyImagesPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.vocabularyImages.list(), []);
  const [images, setImages] = useState<AdminVocabularyImageDto[] | null>(null);
  const [query, setQuery] = useState("");
  const [level, setLevel] = useState("ALL");
  const [replacing, setReplacing] = useState<string | null>(null);
  const [toast, setToast] = useState<ToastState>(null);

  const source = useMemo(() => images ?? data ?? [], [data, images]);
  const filtered = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("uz");
    return source.filter((item) => (
      (level === "ALL" || item.level === level)
      && (!normalized
        || item.word.toLocaleLowerCase("uz").includes(normalized)
        || item.translation.toLocaleLowerCase("uz").includes(normalized)
        || item.topicTitle.toLocaleLowerCase("uz").includes(normalized))
    ));
  }, [level, query, source]);

  const showToast = (next: Exclude<ToastState, null>) => {
    setToast(next);
    window.setTimeout(() => setToast(null), 3600);
  };

  const replace = async (item: AdminVocabularyImageDto) => {
    setReplacing(item.imageId);
    try {
      const updated = await api.admin.vocabularyImages.replace(item.topicId, item.imageId);
      setImages(source.map((candidate) => candidate.imageId === updated.imageId ? updated : candidate));
      showToast({ tone: "success", message: `“${item.word}” rasmi yangilandi.` });
    } catch (replaceError) {
      showToast({
        tone: "danger",
        message: apiErrorDetails(replaceError)?.message ?? "Boshqa xavfsiz rasm topilmadi. Qayta urinib ko‘ring.",
      });
    } finally {
      setReplacing(null);
    }
  };

  if (loading) return <LoadingSkeleton variant="dashboard" label="6000 rasm yuklanmoqda" />;
  if (error || !data) {
    return <ErrorState title="Rasmlarni yuklab bo‘lmadi" description="Admin image katalogi" action={<AppButton onClick={reload}>Qayta urinish</AppButton>} />;
  }

  return (
    <main className="ea-image-manager">
      <PageHeader
        eyebrow="Admin · Vocabulary media"
        title="So‘z rasmlarini boshqarish"
        description="Rasm so‘zga mos kelmasa, almashtirish belgisini bosing. Yangi xavfsiz WebP server va database’da darhol saqlanadi."
        actions={<AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>Admin panel</AppButton>}
      />

      <section className="ea-image-manager__summary" aria-label="Rasm katalogi statistikasi">
        <StatCard icon="add_photo_alternate" label="Jami rasmlar" value={source.length} />
        <StatCard icon="list" label="Ko‘rsatilmoqda" value={filtered.length} />
      </section>

      <section className="ea-image-manager__toolbar" aria-label="Rasmlarni filtrlash">
        <label className="ea-image-manager__search">
          <span>English, o‘zbekcha yoki mavzu bo‘yicha qidirish</span>
          <DesignInput value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Masalan: family, oila, travel" />
        </label>
        <div className="ea-image-manager__levels" role="group" aria-label="CEFR darajasi">
          {["ALL", "A1", "A2", "B1", "B2", "C1", "C2"].map((value) => (
            <button key={value} type="button" className={level === value ? "is-active" : ""} onClick={() => setLevel(value)}>
              {value === "ALL" ? "Barchasi" : value}
            </button>
          ))}
        </div>
      </section>

      <p className="ea-image-manager__instruction">
        <Icon name="check_circle" /> Har bir kartada 100 × 100 px rasm, inglizcha so‘z va o‘zbekcha ma’no ko‘rsatilgan.
      </p>

      {filtered.length === 0 ? (
        <div className="ea-image-manager__empty">Qidiruvga mos rasm topilmadi.</div>
      ) : (
        <section className="ea-image-manager__grid" aria-label="Vocabulary rasmlari">
          {filtered.map((item) => {
            const busy = replacing === item.imageId;
            const src = `${item.imageUrl}?v=${item.version}`;
            return (
              <article className="ea-image-tile" key={item.imageId}>
                <div className="ea-image-tile__media">
                  <img src={src} width="100" height="100" loading="lazy" alt={`${item.word} — ${item.translation}`} />
                  <button
                    type="button"
                    className="ea-image-tile__replace"
                    aria-label={`${item.word} rasmini almashtirish`}
                    title="Mos emas — boshqa xavfsiz rasm tanlash"
                    disabled={busy || replacing !== null}
                    onClick={() => replace(item)}
                  >
                    <Icon name={busy ? "progress_activity" : "add_photo_alternate"} className={busy ? "is-spinning" : ""} />
                  </button>
                </div>
                <div className="ea-image-tile__copy">
                  <strong lang="en">{item.word}</strong>
                  <span lang="uz">{item.translation}</span>
                  <small>{item.level} · {item.topicTitle}</small>
                </div>
              </article>
            );
          })}
        </section>
      )}
      <DesignToast open={toast !== null} title={toast?.message ?? ""} tone={toast?.tone ?? "success"} onClose={() => setToast(null)} />
    </main>
  );
}

export default AdminVocabularyImagesPage;
