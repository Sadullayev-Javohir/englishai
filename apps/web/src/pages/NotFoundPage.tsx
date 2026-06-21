import { useNavigate } from "react-router-dom";
import { Icon } from "@/components/ui/Icon";
import { ParrotLogo } from "@/components/ParrotLogo";
import { uz } from "@/content/uz";
import "./NotFoundPage.css";

export function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <main className="not-found-page">
      <div className="not-found-page__glow not-found-page__glow--purple" aria-hidden="true" />
      <div className="not-found-page__glow not-found-page__glow--blue" aria-hidden="true" />

      <section className="not-found-page__panel" aria-labelledby="not-found-title">
        <div className="not-found-page__brand" aria-label={uz.brand}>
          <ParrotLogo
            size={54}
            imgSize={50}
            rounded="14px"
            className="not-found-page__logo"
            withWordmark
          />
        </div>

        <div className="not-found-page__layout">
          <div className="not-found-page__copy">
            <span className="not-found-page__eyebrow">
              <Icon name="search_off" />
              Yo‘nalish topilmadi
            </span>
            <div className="not-found-page__code" aria-hidden="true">404</div>
            <h1 id="not-found-title">{uz.notFound.title}</h1>
            <p>{uz.notFound.text}</p>

            <div className="not-found-page__actions">
              <button
                type="button"
                className="not-found-page__button not-found-page__button--back"
                onClick={() => navigate(-1)}
              >
                <Icon name="arrow_back" />
                {uz.notFound.back}
              </button>
              <button
                type="button"
                className="not-found-page__button not-found-page__button--home"
                onClick={() => navigate("/home", { replace: true })}
              >
                <Icon name="home" filled />
                {uz.notFound.home}
              </button>
            </div>
          </div>

          <div className="not-found-page__illustration" aria-hidden="true">
            <div className="not-found-page__orbit not-found-page__orbit--outer" />
            <div className="not-found-page__orbit not-found-page__orbit--inner" />
            <div className="not-found-page__planet">
              <span className="not-found-page__planet-dot not-found-page__planet-dot--one" />
              <span className="not-found-page__planet-dot not-found-page__planet-dot--two" />
              <Icon name="travel_explore" />
            </div>
            <span className="not-found-page__satellite not-found-page__satellite--book"><Icon name="menu_book" /></span>
            <span className="not-found-page__satellite not-found-page__satellite--language"><Icon name="translate" /></span>
            <span className="not-found-page__satellite not-found-page__satellite--spark"><Icon name="auto_awesome" filled /></span>
          </div>
        </div>

        <div className="not-found-page__tips" aria-label="Foydali yo‘nalishlar">
          <div className="not-found-page__tip not-found-page__tip--purple">
            <Icon name="route" />
            <span><strong>Manzilni tekshiring</strong>URL yozilishida xato bo‘lishi mumkin.</span>
          </div>
          <div className="not-found-page__tip not-found-page__tip--blue">
            <Icon name="home" />
            <span><strong>Asosiy sahifaga qayting</strong>Darslaringizni davom ettiring.</span>
          </div>
          <div className="not-found-page__tip not-found-page__tip--orange">
            <Icon name="history" />
            <span><strong>Orqaga qayting</strong>Oldingi sahifa hali ochiq bo‘lishi mumkin.</span>
          </div>
        </div>
      </section>
    </main>
  );
}
