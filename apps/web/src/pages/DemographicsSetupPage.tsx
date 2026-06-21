import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { AcquisitionSource, Gender } from "@/api/types";
import { useAuth } from "@/app/auth";
import { returnTargetOr } from "@/app/returnTarget";
import { DesignModal } from "@/components/design";
import { OnboardingChrome, PlayButton, PlayCompanion, PlayOption } from "./onboarding/OnboardingChrome";
import { Icon } from "@/components/ui/Icon";
import "./DemographicsSetupPage.css";

const months = ["Yanvar", "Fevral", "Mart", "Aprel", "May", "Iyun", "Iyul", "Avgust", "Sentabr", "Oktabr", "Noyabr", "Dekabr"] as const;

const sources = [
  [AcquisitionSource.Telegram, "Telegram", "send"],
  [AcquisitionSource.Instagram, "Instagram", "photo_camera"],
  [AcquisitionSource.Google, "Google", "search"],
  [AcquisitionSource.AiAssistants, "AI yordamchilari", "smart_toy"],
  [AcquisitionSource.FriendReferral, "Do‘st-tanish", "group"],
  [AcquisitionSource.Other, "Boshqa", "more_horiz"],
] as const;

type DateField = "day" | "month" | "year";

function currentParts(value: string | null) {
  if (!value) return { day: "", month: "", year: "" };
  const [year, month, day] = value.split("-");
  return { day: String(Number(day)), month: String(Number(month)), year };
}

export function DemographicsSetupPage() {
  const { user, applyUser } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const isProfileEdit = new URLSearchParams(location.search).get("edit") === "1";
  const initial = currentParts(user?.birthDate ?? null);
  const [day, setDay] = useState(initial.day);
  const [month, setMonth] = useState(initial.month);
  const [year, setYear] = useState(initial.year);
  const [openDateField, setOpenDateField] = useState<DateField | null>(null);
  const [gender, setGender] = useState<Gender | null>(user?.gender ?? null);
  const [source, setSource] = useState<AcquisitionSource | null>(user?.acquisitionSource ?? null);
  const [other, setOther] = useState(user?.acquisitionSourceOther ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const currentYear = new Date().getFullYear();
  const years = useMemo(() => Array.from({ length: 121 }, (_, index) => currentYear - index), [currentYear]);
  const daysInMonth = year && month ? new Date(Number(year), Number(month), 0).getDate() : 31;
  const validOther = source !== AcquisitionSource.Other || other.trim().length >= 2;
  const canSubmit = Boolean(day && month && year && gender && source && validOther && !saving);

  const dateModal = openDateField ? {
    day: {
      title: "Kunni tanlang",
      description: "Tug‘ilgan kuningizni belgilang.",
      values: Array.from({ length: daysInMonth }, (_, index) => ({ value: String(index + 1), label: String(index + 1) })),
      selected: day,
    },
    month: {
      title: "Oyni tanlang",
      description: "Tug‘ilgan oyingizni belgilang.",
      values: months.map((label, index) => ({ value: String(index + 1), label })),
      selected: month,
    },
    year: {
      title: "Yilni tanlang",
      description: "Tug‘ilgan yilingizni belgilang.",
      values: years.map((value) => ({ value: String(value), label: String(value) })),
      selected: year,
    },
  }[openDateField] : null;

  function selectDateValue(value: string) {
    if (openDateField === "day") setDay(value);
    if (openDateField === "month") {
      setMonth(value);
      const availableDays = new Date(Number(year || currentYear), Number(value), 0).getDate();
      if (Number(day) > availableDays) setDay("");
    }
    if (openDateField === "year") {
      setYear(value);
      if (month && Number(day) > new Date(Number(value), Number(month), 0).getDate()) setDay("");
    }
    setOpenDateField(null);
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (!canSubmit || !gender || !source) return;
    setSaving(true);
    setError(null);
    const birthDate = `${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")}`;
    try {
      const updated = await api.auth.setDemographics(birthDate, gender, source, other.trim() || null);
      applyUser(updated);
      navigate(isProfileEdit ? "/profile" : returnTargetOr(updated.hasOnboarded ? "/home" : "/welcome"), { replace: true });
    } catch {
      setError("Ma’lumotlarni saqlab bo‘lmadi. Qiymatlarni tekshirib, qayta urinib ko‘ring.");
      setSaving(false);
    }
  }

  return (
    <OnboardingChrome className="demographics-page" step={isProfileEdit ? undefined : 2} backTo={isProfileEdit ? "/profile" : "/username"}>
      <div className="play-profile">
        <PlayCompanion className="play-profile__companion" message={<>Har bir o‘rganuvchi o‘zgacha.<br />Siz ham!</>} secure />
        <form className="play-profile__form" onSubmit={submit}>
          <div className="play-profile__heading">
            <h1 className="onboarding-play__title">{isProfileEdit ? "Profil ma’lumotlari" : <>Siz haqingizda<br />ozgina bilsak.</>}</h1>
            <p className="onboarding-play__lede">Uchta kichik savol. Tamom.</p>
          </div>
          <fieldset className="play-profile__fieldset">
            <legend>Tug‘ilgan sanangiz</legend>
            <div className="play-profile__date">
              <label>Kun<button type="button" className={!day ? "is-placeholder" : ""} onClick={() => setOpenDateField("day")} aria-haspopup="dialog"><span>{day || "Kun"}</span><Icon name="chevron_down" /></button></label>
              <label>Oy<button type="button" className={!month ? "is-placeholder" : ""} onClick={() => setOpenDateField("month")} aria-haspopup="dialog"><span>{month ? months[Number(month) - 1] : "Oy"}</span><Icon name="chevron_down" /></button></label>
              <label>Yil<button type="button" className={!year ? "is-placeholder" : ""} onClick={() => setOpenDateField("year")} aria-haspopup="dialog"><span>{year || "Yil"}</span><Icon name="chevron_down" /></button></label>
            </div>
          </fieldset>
          <fieldset className="play-profile__fieldset">
            <legend>Jinsingiz</legend>
            <div className="play-profile__gender">
              {[[Gender.Male, "Erkak", "mars"], [Gender.Female, "Ayol", "venus"]].map(([value, label, icon]) => (
                <PlayOption compact key={value} selected={gender === value} onClick={() => setGender(value as Gender)} icon={<Icon name={String(icon)} />} disabled={saving}>{label}</PlayOption>
              ))}
            </div>
          </fieldset>
          <fieldset className="play-profile__fieldset">
            <legend>Bizni qayerdan topdingiz?</legend>
            <div className="play-profile__sources">
              {sources.map(([value, label, icon]) => <PlayOption compact key={value} selected={source === value} onClick={() => setSource(value)} icon={<Icon name={icon} />} disabled={saving}>{label}</PlayOption>)}
            </div>
            {source === AcquisitionSource.Other && <label className="play-profile__other">Manbani yozing<input className="onboarding-play__input" value={other} maxLength={100} onChange={(event) => setOther(event.target.value)} placeholder="Masalan: tadbir, blog yoki boshqa joy" /></label>}
          </fieldset>
          {error && <p className="onboarding-play__alert" role="alert">{error}</p>}
          <PlayButton disabled={!canSubmit} type="submit" arrow={!saving}>{saving ? "Saqlanmoqda..." : isProfileEdit ? "O‘zgarishlarni saqlash" : "Saqlash va davom etish"}</PlayButton>
        </form>
      </div>

      <DesignModal
        open={Boolean(openDateField && dateModal)}
        onClose={() => setOpenDateField(null)}
        title={dateModal?.title ?? ""}
        description={dateModal?.description}
        className="demographics-select-modal"
      >
        <div className={`demographics-select-grid demographics-select-grid--${openDateField ?? "day"}`}>
          {dateModal?.values.map(({ value, label }) => (
            <button
              type="button"
              className={dateModal.selected === value ? "is-selected" : ""}
              onClick={() => selectDateValue(value)}
              aria-pressed={dateModal.selected === value}
              key={value}
            >
              <span>{label}</span>
              {dateModal.selected === value && <Icon name="check" />}
            </button>
          ))}
        </div>
      </DesignModal>
    </OnboardingChrome>
  );
}
