import { useEffect, useId, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import { AdminRole } from "@/api/types";
import type { AdminUserDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import {
  AppButton,
  AppIconButton,
  DesignCard,
  DesignConfirm,
  DesignInput,
  DesignModal,
  EmptyState,
  ErrorState,
  FormField,
  PageHeader,
  ResponsiveGrid,
  StatCard,
} from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { uz } from "@/content/uz";
import { formatDate, formatDateTime } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";

type RoleFilter = "all" | "admins" | "learners";
type SubscriptionFilter = "all" | "Premium" | "Free" | "Cancelled" | "Expired";
const PAGE_SIZE = 8;

export function AdminUsersPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.users(undefined, 50), []);
  const [loadedUsers, setLoadedUsers] = useState<AdminUserDto[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loadingMore, setLoadingMore] = useState(false);
  const [query, setQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState<RoleFilter>("all");
  const [subscriptionFilter, setSubscriptionFilter] = useState<SubscriptionFilter>("all");
  const [page, setPage] = useState(1);
  const [pendingUser, setPendingUser] = useState<AdminUserDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [actionError, setActionError] = useState(false);

  useEffect(() => {
    if (!data) return;
    setLoadedUsers(data.users);
    setNextCursor(data.nextCursor);
  }, [data]);

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return loadedUsers.filter((user) => {
      const matchesQuery = !normalizedQuery
        || [user.displayName, user.email, user.username ?? ""].some((value) => value.toLowerCase().includes(normalizedQuery));
      const matchesRole = roleFilter === "all"
        || (roleFilter === "admins" && user.role !== AdminRole.None)
        || (roleFilter === "learners" && user.role === AdminRole.None);
      const matchesSubscription = subscriptionFilter === "all" || user.subscriptionStatus === subscriptionFilter;
      return matchesQuery && matchesRole && matchesSubscription;
    });
  }, [loadedUsers, query, roleFilter, subscriptionFilter]);

  const loadMore = async () => {
    if (!nextCursor || loadingMore) return;
    setLoadingMore(true);
    try {
      const next = await api.admin.users(nextCursor, 50);
      setLoadedUsers((current) => [...current, ...next.users]);
      setNextCursor(next.nextCursor);
    } finally {
      setLoadingMore(false);
    }
  };

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const visibleUsers = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
  const forbidden = (error as { status?: number } | null)?.status === 403;
  const resetPage = () => setPage(1);

  const confirmRoleChange = async () => {
    if (!pendingUser) return;
    setSaving(true);
    setActionError(false);
    try {
      await api.admin.setAdmin(pendingUser.id, pendingUser.role !== AdminRole.Admin);
      setPendingUser(null);
      reload();
    } catch {
      setActionError(true);
    } finally {
      setSaving(false);
    }
  };

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Admin boshqaruvi"
        title={uz.admin.usersTitle}
        description={uz.admin.usersSubtitle}
        actions={<AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>Orqaga</AppButton>}
      />

      {loading ? <LoadingSkeleton variant="table" rows={7} /> : error || !data ? (
        <ErrorState
          title={forbidden ? uz.admin.forbidden : uz.admin.loadError}
          icon={forbidden ? "lock" : "cloud_off"}
          action={forbidden ? undefined : <AppButton tone="standard" onClick={reload}>Qayta urinish</AppButton>}
        />
      ) : (
        <>
          <ResponsiveGrid minItemWidth={210} data-accent="users" aria-label="Foydalanuvchilar statistikasi">
            <StatCard icon="group" label={uz.admin.totalUsers} value={data.totalUsers} />
            <StatCard icon="shield_person" label={uz.admin.admins} value={data.adminCount} />
            <StatCard icon="rocket_launch" label={uz.admin.onboarded} value={data.onboardedCount} />
            <StatCard icon="workspace_premium" label={uz.admin.premium} value={data.premiumCount} />
          </ResponsiveGrid>

          <DesignCard as="section" className="space-y-5" aria-labelledby="users-directory-title">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <span className="ea-eyebrow">Hisoblar katalogi</span>
                <h2 id="users-directory-title" className="text-responsive-heading mt-2">{uz.admin.usersTitle}</h2>
              </div>
              <span className="w-fit rounded-lg bg-ea-surface-soft px-3 py-2 text-sm font-bold text-ea-muted">
                {filtered.length} / {data.totalUsers}
              </span>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
              <FormField label={uz.admin.search} htmlFor="admin-users-search">
                <DesignInput
                  id="admin-users-search"
                  type="search"
                  value={query}
                  placeholder={uz.admin.search}
                  onChange={(event) => { setQuery(event.target.value); resetPage(); }}
                />
              </FormField>
              <Filter
                label="Rol"
                value={roleFilter}
                onChange={(value) => { setRoleFilter(value as RoleFilter); resetPage(); }}
                options={[["all", "Barcha rollar"], ["admins", uz.admin.admins], ["learners", uz.admin.roleLearner]]}
              />
              <Filter
                label="Obuna"
                value={subscriptionFilter}
                onChange={(value) => { setSubscriptionFilter(value as SubscriptionFilter); resetPage(); }}
                options={[["all", "Barcha obunalar"], ["Premium", uz.admin.subPremium], ["Free", uz.admin.subFree], ["Cancelled", uz.admin.subCancelled], ["Expired", uz.admin.subExpired]]}
              />
            </div>

            {visibleUsers.length === 0 ? (
              <EmptyState title={uz.admin.empty} icon="search_off" />
            ) : (
              <UserDirectory
                users={visibleUsers}
                canManage={data.viewerRole === AdminRole.SuperAdmin}
                currentUserId={getLearnerId()}
                onManage={(user) => { setPendingUser(user); setActionError(false); }}
              />
            )}

            <div className="flex flex-col gap-3 border-t border-ea-border pt-4 sm:flex-row sm:items-center sm:justify-between">
              <Pagination page={currentPage} totalPages={totalPages} onChange={setPage} />
              {nextCursor && (
                <AppButton tone="standard" loading={loadingMore} onClick={() => void loadMore()}>
                  {loadingMore ? "Yuklanmoqda..." : "Yana yuklash"}
                </AppButton>
              )}
            </div>
          </DesignCard>
        </>
      )}

      <DesignConfirm
        open={pendingUser !== null}
        onClose={() => { if (!saving) { setPendingUser(null); setActionError(false); } }}
        title={pendingUser?.role === AdminRole.Admin ? uz.admin.removeAdmin : uz.admin.makeAdmin}
        description={pendingUser?.displayName}
        message={(
          <div className="space-y-3">
            <p>
              {pendingUser
                ? (pendingUser.role === AdminRole.Admin
                    ? uz.admin.confirmRemoveAdmin(pendingUser.displayName)
                    : uz.admin.confirmMakeAdmin(pendingUser.displayName))
                : ""}
            </p>
            {actionError && <p className="text-sm font-bold text-ea-danger" role="alert">{uz.admin.updateError}</p>}
          </div>
        )}
        confirmLabel="Tasdiqlash"
        destructive={pendingUser?.role === AdminRole.Admin}
        loading={saving}
        closeOnBackdrop={!saving}
        closeOnEscape={!saving}
        onConfirm={() => void confirmRoleChange()}
      />
    </main>
  );
}

function Filter({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: Array<[string, string]>;
  onChange: (value: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const triggerId = useId();
  const currentLabel = options.find(([optionValue]) => optionValue === value)?.[1] ?? label;

  return (
    <FormField label={label} htmlFor={triggerId}>
      <AppButton
        id={triggerId}
        tone="standard"
        fullWidth
        trailingIcon="expand_more"
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-label={`${label}: ${currentLabel}`}
        onClick={() => setOpen(true)}
      >
        {currentLabel}
      </AppButton>
      <DesignModal
        open={open}
        onClose={() => setOpen(false)}
        title={label}
        description="Filtr qiymatini tanlang"
        closeLabel="Yopish"
      >
        <div className="grid gap-2">
          {options.map(([optionValue, optionLabel]) => (
            <AppButton
              key={optionValue}
              tone={value === optionValue ? "performance" : "standard"}
              leadingIcon={value === optionValue ? "check" : "tune"}
              fullWidth
              onClick={() => {
                onChange(optionValue);
                setOpen(false);
              }}
            >
              {optionLabel}
            </AppButton>
          ))}
        </div>
      </DesignModal>
    </FormField>
  );
}

function UserDirectory({
  users,
  canManage,
  currentUserId,
  onManage,
}: {
  users: AdminUserDto[];
  canManage: boolean;
  currentUserId: string;
  onManage: (user: AdminUserDto) => void;
}) {
  return (
    <>
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full min-w-[980px] border-collapse text-left">
          <thead>
            <tr className="border-b border-ea-border text-xs uppercase tracking-wide text-ea-muted">
              <th className="px-3 py-3">{uz.admin.colUser}</th>
              <th className="px-3 py-3">{uz.admin.colRole}</th>
              <th className="px-3 py-3">{uz.admin.colSubscription}</th>
              <th className="px-3 py-3">{uz.admin.colLevel}</th>
              <th className="px-3 py-3">{uz.admin.colLastLogin}</th>
              <th className="px-3 py-3 text-right">{uz.admin.colActions}</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="border-b border-ea-border last:border-b-0">
                <td className="px-3 py-4"><Identity user={user} isSelf={user.id === currentUserId} /></td>
                <td className="px-3 py-4"><RoleBadge role={user.role} /></td>
                <td className="px-3 py-4"><SubscriptionBadge status={user.subscriptionStatus} /></td>
                <td className="px-3 py-4 text-sm font-bold text-ea-text">{user.level ?? uz.admin.notOnboarded}</td>
                <td className="px-3 py-4"><LastLogin value={user.lastLoginAt} /></td>
                <td className="px-3 py-4 text-right"><ManageButton user={user} canManage={canManage} onManage={onManage} compact /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ResponsiveGrid minItemWidth={280} className="xl:hidden">
        {users.map((user) => (
          <DesignCard as="article" key={user.id} padding="sm" className="space-y-4">
            <Identity user={user} isSelf={user.id === currentUserId} />
            <div className="flex flex-wrap gap-2">
              <RoleBadge role={user.role} />
              <SubscriptionBadge status={user.subscriptionStatus} />
            </div>
            <dl className="grid grid-cols-2 gap-3 border-t border-ea-border pt-3 text-sm">
              <div><dt className="text-ea-muted">{uz.admin.colLevel}</dt><dd className="mt-1 font-bold text-ea-text">{user.level ?? uz.admin.notOnboarded}</dd></div>
              <div><dt className="text-ea-muted">{uz.admin.colRegistered}</dt><dd className="mt-1 font-bold text-ea-text">{formatDate(user.registeredAt)}</dd></div>
              <div className="col-span-2"><dt className="text-ea-muted">{uz.admin.colLastLogin}</dt><dd className="mt-1"><LastLogin value={user.lastLoginAt} /></dd></div>
            </dl>
            <ManageButton user={user} canManage={canManage} onManage={onManage} />
          </DesignCard>
        ))}
      </ResponsiveGrid>
    </>
  );
}

function LastLogin({ value }: { value: string }) {
  const dateTime = formatDateTime(value);
  const time = dateTime.split(" ")[1] ?? "";
  return (
    <span className="inline-flex flex-col text-sm">
      <strong className="text-ea-text">{formatDate(value)}</strong>
      {time && <small className="text-ea-muted">{time}</small>}
    </span>
  );
}

function Identity({ user, isSelf }: { user: AdminUserDto; isSelf: boolean }) {
  return (
    <Link to={`/admin/users/${user.id}`} className="flex min-w-0 items-center gap-3 rounded-lg text-ea-text outline-none focus-visible:ring-2 focus-visible:ring-ea-focus">
      {user.pictureUrl ? (
        <img className="h-11 w-11 shrink-0 rounded-full object-cover" src={user.pictureUrl} alt="" referrerPolicy="no-referrer" />
      ) : (
        <span className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-ea-primary-soft font-extrabold text-ea-primary-deep" aria-hidden="true">
          {(user.displayName || user.email).charAt(0).toUpperCase()}
        </span>
      )}
      <span className="min-w-0 flex-1">
        <span className="flex flex-wrap items-center gap-2 font-bold">
          {user.displayName}
          {isSelf && <em className="rounded-full bg-ea-surface-soft px-2 py-0.5 text-[10px] not-italic text-ea-muted">{uz.admin.youBadge}</em>}
        </span>
        <span className="block truncate text-sm text-ea-muted">{user.email}</span>
        {user.username && <small className="block text-ea-muted">@{user.username}</small>}
      </span>
      <Icon name="chevron_right" className="shrink-0 text-ea-muted" />
    </Link>
  );
}

function RoleBadge({ role }: { role: AdminRole }) {
  const label = role === AdminRole.SuperAdmin
    ? uz.admin.roleSuperAdmin
    : role === AdminRole.Admin
      ? uz.admin.roleAdmin
      : uz.admin.roleLearner;
  const admin = role !== AdminRole.None;
  return (
    <span className={admin
      ? "inline-flex items-center gap-1.5 rounded-full bg-ea-primary-soft px-3 py-1 text-xs font-bold text-ea-primary-deep"
      : "inline-flex items-center gap-1.5 rounded-full bg-ea-surface-soft px-3 py-1 text-xs font-bold text-ea-muted"}
    >
      <Icon name={admin ? "shield" : "person"} filled />{label}
    </span>
  );
}

function SubscriptionBadge({ status }: { status: string }) {
  const premium = status === "Premium";
  const danger = status === "Cancelled" || status === "Expired";
  const label = premium
    ? uz.admin.subPremium
    : status === "Cancelled"
      ? uz.admin.subCancelled
      : status === "Expired"
        ? uz.admin.subExpired
        : uz.admin.subFree;
  return (
    <span className={danger
      ? "inline-flex items-center gap-1.5 rounded-full bg-ea-orange-50 px-3 py-1 text-xs font-bold text-ea-orange-600"
      : premium
        ? "inline-flex items-center gap-1.5 rounded-full bg-ea-green-50 px-3 py-1 text-xs font-bold text-ea-green-600"
        : "inline-flex items-center gap-1.5 rounded-full bg-ea-surface-soft px-3 py-1 text-xs font-bold text-ea-muted"}
    >
      <Icon name={premium ? "workspace_premium" : "payments"} />{label}
    </span>
  );
}

function ManageButton({
  user,
  canManage,
  onManage,
  compact = false,
}: {
  user: AdminUserDto;
  canManage: boolean;
  onManage: (user: AdminUserDto) => void;
  compact?: boolean;
}) {
  if (!canManage || user.role === AdminRole.SuperAdmin) {
    return compact ? <Icon name="lock" className="text-ea-muted" /> : null;
  }
  const isAdmin = user.role === AdminRole.Admin;
  if (compact) {
    return (
      <AppIconButton
        icon={isAdmin ? "person_remove" : "person_add"}
        label={isAdmin ? uz.admin.removeAdmin : uz.admin.makeAdmin}
        tone={isAdmin ? "danger" : "standard"}
        onClick={() => onManage(user)}
      />
    );
  }
  return (
    <AppButton
      fullWidth
      tone={isAdmin ? "danger" : "standard"}
      leadingIcon={isAdmin ? "person_remove" : "person_add"}
      onClick={() => onManage(user)}
    >
      {isAdmin ? uz.admin.removeAdmin : uz.admin.makeAdmin}
    </AppButton>
  );
}

function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (page: number) => void }) {
  if (totalPages <= 1) return <span />;
  return (
    <nav className="flex items-center gap-3" aria-label="Sahifalash">
      <AppIconButton icon="chevron_left" label="Oldingi sahifa" disabled={page === 1} onClick={() => onChange(page - 1)} />
      <span className="text-sm font-bold text-ea-muted">{page} / {totalPages}</span>
      <AppIconButton icon="chevron_right" label="Keyingi sahifa" disabled={page === totalPages} onClick={() => onChange(page + 1)} />
    </nav>
  );
}
