import { Suspense, lazy } from "react";
import { createBrowserRouter, Navigate, Outlet, useLocation, useParams } from "react-router-dom";
import {
  ShellLayout,
  RequireAuth,
  OnboardingGate,
  RequireOnboarded,
  RequireUsername,
  UsernameGate,
  RequireDemographics,
  DemographicsGate,
  GoalGate,
  OnboardingGoalGate,
  RequireAdmin,
  RequireSuperAdmin,
  AdminAreaLayout,
  PublicPageLayout,
  OnboardingPageLayout,
} from "./layout";
import { nativeRouteAllowed } from "@/api/nativeCapabilities";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { LessonFoundationPreview } from "@/components/lesson/LessonFoundationPreview";
import { OnboardingPreview } from "@/pages/onboarding/OnboardingPreview";
import { RequireMandatoryVocabularyReview } from "./MandatoryReviewGate";
import { DocumentTitleProvider } from "./documentTitle";
import { lazyPage } from "./lazyPage";
import { RootEntry } from "./RootEntry";

import { LandingPage } from "@/pages/LandingPage";
import { PricingPage } from "@/pages/PricingPage";
import { PublicContentPage } from "@/pages/PublicContentPage";
import { PublicSeoPage } from "@/pages/PublicSeoPage";

function LegacyAppRedirect({ target }: { target: string }) {
  const location = useLocation();
  const { "*": remainder = "" } = useParams();
  const suffix = remainder ? `/${remainder}` : "";
  return <Navigate to={`${target}${suffix}${location.search}${location.hash}`} replace />;
}

function NativeCapabilityRoute({ path, children }: { path: string; children: React.ReactNode }) {
  return nativeRouteAllowed(path) ? children : <Navigate to="/home" replace />;
}

const HeroExplainPage = lazyPage(
  () => import("@/pages/HeroExplainPage"),
  "HeroExplainPage"
);
const NotFoundPage = lazyPage(
  () => import("@/pages/NotFoundPage"),
  "NotFoundPage"
);
const HomePage = lazyPage(() => import("@/pages/HomePage"), "HomePage");
const LoginPage = lazyPage(() => import("@/pages/LoginPage"), "LoginPage");
const UsernameSetupPage = lazyPage(
  () => import("@/pages/UsernameSetupPage"),
  "UsernameSetupPage"
);
const DemographicsSetupPage = lazyPage(
  () => import("@/pages/DemographicsSetupPage"),
  "DemographicsSetupPage"
);
const WelcomePage = lazyPage(
  () => import("@/pages/WelcomePage"),
  "WelcomePage"
);
const OnboardingGoalPage = lazyPage(
  () => import("@/pages/OnboardingGoalPage"),
  "OnboardingGoalPage"
);
const AssessmentIntroPage = lazyPage(
  () => import("@/pages/AssessmentIntroPage"),
  "AssessmentIntroPage"
);
const PlacementTestPage = lazyPage(
  () => import("@/pages/PlacementTestPage"),
  "PlacementTestPage"
);
const PlacementResultPage = lazyPage(
  () => import("@/pages/PlacementResultPage"),
  "PlacementResultPage"
);
const SpeakingPage = lazyPage(
  () => import("@/pages/SpeakingPage"),
  "SpeakingPage"
);
const SpeakingTopicsPage = lazyPage(
  () => import("@/pages/SpeakingTopicsPage"),
  "SpeakingTopicsPage"
);
const LiveAccentTutorPage = lazyPage(
  () => import("@/pages/LiveAccentTutorPage"),
  "LiveAccentTutorPage"
);
const FreeTalkTopicsPage = lazyPage(
  () => import("@/pages/FreeTalkTopicsPage"),
  "FreeTalkTopicsPage"
);
const RoleTalkScenariosPage = lazyPage(
  () => import("@/pages/RoleTalkScenariosPage"),
  "RoleTalkScenariosPage"
);
const LegacyPronunciationRedirect = lazyPage(
  () => import("@/pages/PronunciationDetailPage"),
  "LegacyPronunciationRedirect"
);
const PronunciationDetailPage = lazyPage(
  () => import("@/pages/PronunciationDetailPage"),
  "PronunciationDetailPage"
);
const SpeakingPracticeWordsPage = lazyPage(
  () => import("@/pages/SpeakingPracticeWordsPage"),
  "SpeakingPracticeWordsPage"
);
const VideoCatalogPage = lazyPage(
  () => import("@/pages/VideoCatalogPage"),
  "VideoCatalogPage"
);
const VideoPlayerPage = lazyPage(
  () => import("@/pages/VideoPlayerPage"),
  "VideoPlayerPage"
);
const VideoFullSearchPage = lazyPage(
  () => import("@/pages/VideoFullSearchPage"),
  "VideoFullSearchPage"
);
const VideoPlaylistPage = lazyPage(
  () => import("@/pages/VideoPlaylistPage"),
  "VideoPlaylistPage"
);
const VideoPlaylistPlayerPage = lazyPage(
  () => import("@/pages/VideoPlaylistPlayerPage"),
  "VideoPlaylistPlayerPage"
);
const VideoQuizPage = lazyPage(
  () => import("@/pages/VideoQuizPage"),
  "VideoQuizPage"
);
const VocabularyTopicsPage = lazyPage(
  () => import("@/pages/VocabularyTopicsPage"),
  "VocabularyTopicsPage"
);
const VocabularyTopicPage = lazyPage(
  () => import("@/pages/VocabularyTopicPage"),
  "VocabularyTopicPage"
);
const VocabularyPronunciationPage = lazyPage(
  () => import("@/pages/VocabularyPronunciationPage"),
  "VocabularyPronunciationPage"
);
const VocabularyReviewPage = lazyPage(
  () => import("@/pages/VocabularyReviewPage"),
  "VocabularyReviewPage"
);
const MySavedWordsPage = lazyPage(
  () => import("@/pages/MySavedWordsPage"),
  "MySavedWordsPage"
);
const SavedVocabularyTopicPracticePage = lazyPage(
  () => import("@/pages/SavedVocabularyTopicPracticePage"),
  "SavedVocabularyTopicPracticePage"
);
const LevelMapPage = lazyPage(
  () => import("@/pages/LevelMapPage"),
  "LevelMapPage"
);
const ExitTestPage = lazyPage(
  () => import("@/pages/ExitTestPage"),
  "ExitTestPage"
);
const ExitTestResultPage = lazyPage(
  () => import("@/pages/ExitTestResultPage"),
  "ExitTestResultPage"
);
const ProgressPage = lazyPage(
  () => import("@/pages/ProgressPage"),
  "ProgressPage"
);
const LeaderboardPage = lazyPage(
  () => import("@/pages/LeaderboardPage"),
  "LeaderboardPage"
);
const LeaderboardLearnerPage = lazyPage(
  () => import("@/pages/LeaderboardLearnerPage"),
  "LeaderboardLearnerPage"
);
const ProfilePage = lazyPage(
  () => import("@/pages/ProfilePage"),
  "ProfilePage"
);
const ReadingCatalogPage = lazyPage(
  () => import("@/pages/ReadingCatalogPage"),
  "ReadingCatalogPage"
);
const BooksCatalogPage = lazyPage(
  () => import("@/pages/BooksCatalogPage"),
  "BooksCatalogPage"
);
const BookDetailPage = lazyPage(
  () => import("@/pages/BookDetailPage"),
  "BookDetailPage"
);
const BookSectionPage = lazyPage(
  () => import("@/pages/BookSectionPage"),
  "BookSectionPage"
);
const ListeningCatalogPage = lazyPage(
  () => import("@/pages/ListeningCatalogPage"),
  "ListeningCatalogPage"
);
const ListeningTopicPage = lazyPage(
  () => import("@/pages/ListeningTopicPage"),
  "ListeningTopicPage"
);
const ReadingTopicPage = lazyPage(
  () => import("@/pages/ReadingTopicPage"),
  "ReadingTopicPage"
);
const GrammarCatalogPage = lazyPage(
  () => import("@/pages/GrammarCatalogPage"),
  "GrammarCatalogPage"
);
const GrammarLessonPage = lazyPage(
  () => import("@/pages/GrammarLessonPage"),
  "GrammarLessonPage"
);
const WritingCatalogPage = lazyPage(
  () => import("@/pages/WritingCatalogPage"),
  "WritingCatalogPage"
);
const WritingTaskPage = lazyPage(
  () => import("@/pages/WritingTaskPage"),
  "WritingTaskPage"
);
const AdminPage = lazyPage(() => import("@/pages/AdminPage"), "AdminPage");
const AdminUsersPage = lazyPage(
  () => import("@/pages/AdminUsersPage"),
  "AdminUsersPage"
);
const AdminUserDetailPage = lazyPage(
  () => import("@/pages/AdminUserDetailPage"),
  "AdminUserDetailPage"
);
const AdminVocabularyPage = lazyPage(
  () => import("@/pages/AdminVocabularyPage"),
  "AdminVocabularyPage"
);
const AdminVocabularyEditPage = lazyPage(
  () => import("@/pages/AdminVocabularyEditPage"),
  "AdminVocabularyEditPage"
);
const AdminVocabularyImagesPage = lazyPage(
  () => import("@/pages/AdminVocabularyImagesPage"),
  "AdminVocabularyImagesPage"
);
const AdminCurriculumPage = lazyPage(() => import("@/pages/AdminCurriculumPage"), "AdminCurriculumPage");
const AdminGrammarPage = lazyPage(
  () => import("@/pages/AdminGrammarPage"),
  "AdminGrammarPage"
);
const AdminGrammarEditPage = lazyPage(
  () => import("@/pages/AdminGrammarEditPage"),
  "AdminGrammarEditPage"
);
const AdminListeningPage = lazyPage(
  () => import("@/pages/AdminListeningPage"),
  "AdminListeningPage"
);
const AdminListeningEditPage = lazyPage(
  () => import("@/pages/AdminListeningEditPage"),
  "AdminListeningEditPage"
);
const AdminReadingPage = lazyPage(
  () => import("@/pages/AdminReadingPage"),
  "AdminReadingPage"
);
const AdminReadingEditPage = lazyPage(
  () => import("@/pages/AdminReadingEditPage"),
  "AdminReadingEditPage"
);
const AdminNotificationsPage = lazyPage(
  () => import("@/pages/AdminNotificationsPage"),
  "AdminNotificationsPage"
);
const AdminSectionsPage = lazyPage(
  () => import("@/pages/AdminSectionsPage"),
  "AdminSectionsPage"
);
const SupportChatPage = lazyPage(() => import("@/pages/SupportChatPage"), "SupportChatPage");
const AdminSupportPage = lazyPage(() => import("@/pages/AdminSupportPage"), "AdminSupportPage");
const FounderDashboardPage = lazyPage(
  () => import("@/pages/FounderDashboardPage"),
  "FounderDashboardPage"
);
// Lazy - the super-admin server-operations page is opened rarely and by one account.
const ServerHealthPage = lazy(() =>
  import("@/pages/ServerHealthPage").then((m) => ({
    default: m.ServerHealthPage,
  }))
);

export const router = createBrowserRouter([
  {
    element: (
      <DocumentTitleProvider>
        <Outlet />
      </DocumentTitleProvider>
    ),
    children: [
      // Web: the canonical root is the marketing page for guests and a smart app entry for
      // authenticated learners. /landing remains an explicit way to revisit the marketing page.
      // Native (Capacitor): there is no marketing site inside the app shell, so the root goes home.
      {
        element: <PublicPageLayout />,
        children: [
          { path: "/", element: <RootEntry /> },
          { path: "/landing", element: <LandingPage /> },
          { path: "/landing/eng", element: <LandingPage locale="en" /> },
          { path: "/en/", element: <LandingPage locale="en" /> },
          { path: "/ingliz-tilini-organish", element: <PublicSeoPage /> },
          { path: "/en/learn-english", element: <PublicSeoPage /> },
          { path: "/ai-ingliz-tutor", element: <PublicSeoPage /> },
          { path: "/en/ai-english-tutor", element: <PublicSeoPage /> },
          { path: "/vocabulary", element: <PublicSeoPage /> },
          { path: "/en/vocabulary", element: <PublicSeoPage /> },
          { path: "/grammar", element: <PublicSeoPage /> },
          { path: "/en/grammar", element: <PublicSeoPage /> },
          { path: "/cefr", element: <PublicSeoPage /> },
          { path: "/en/cefr", element: <PublicSeoPage /> },
          { path: "/ielts", element: <PublicSeoPage /> },
          { path: "/en/ielts", element: <PublicSeoPage /> },
          { path: "/speaking/*", element: <LegacyAppRedirect target="/app/speaking" /> },
          { path: "/vocabulary/*", element: <LegacyAppRedirect target="/app/vocabulary" /> },
          { path: "/grammar/*", element: <LegacyAppRedirect target="/app/grammar" /> },
          { path: "/learn", element: <PublicContentPage /> },
          { path: "/learn/:slug", element: <PublicContentPage /> },
          { path: "/methodology", element: <PublicContentPage /> },
          { path: "/about", element: <PublicContentPage /> },
          {
            path: "/pricing",
            // Real prices + a waitlist while checkout is closed. Still wrapped in
            // NativeCapabilityRoute: Play policy keeps pricing out of the Android build.
            element: <NativeCapabilityRoute path="/pricing"><PricingPage /></NativeCapabilityRoute>,
          },
          { path: "/product", element: <PublicContentPage /> },
          { path: "/contact", element: <PublicContentPage /> },
          { path: "/editorial-policy", element: <PublicContentPage /> },
          { path: "/login", element: <LoginPage /> },
          { path: "/hero", element: <HeroExplainPage /> },
          ...(import.meta.env.DEV
            ? [
                { path: "/dev/lesson-foundation", element: <LessonFoundationPreview /> },
                { path: "/dev/onboarding/:page", element: <OnboardingPreview /> },
              ]
            : []),
        ],
      },

  {
    element: <RequireAuth />,
    children: [
      // Post-sign-up handle setup - the only screen reachable before a username is chosen.
      {
        element: <UsernameGate />,
        children: [
          {
            element: <OnboardingPageLayout />,
            children: [{ path: "/username", element: <UsernameSetupPage /> }],
          },
        ],
      },
      {
        // Everything past sign-in also requires a username; a brand-new account is sent to
        // /username before onboarding or any app screen can render.
        element: <RequireUsername />,
        children: [
          {
            element: <DemographicsGate />,
            children: [
              {
                element: <OnboardingPageLayout />,
                children: [{ path: "/onboarding/profile-details", element: <DemographicsSetupPage /> }],
              },
            ],
          },
          {
            element: <RequireDemographics />,
            children: [
          // Goal-based onboarding: the one-screen goal picker. Its own gate bounces learners who
          // already chose a goal, skipped, or onboarded, so it can never be re-entered by hand.
          {
            element: <OnboardingGoalGate />,
            children: [
              {
                element: <OnboardingPageLayout />,
                children: [{ path: "/onboarding/goal", element: <OnboardingGoalPage /> }],
              },
            ],
          },
          // Onboarding flow - only reachable until the learner has a level; OnboardingGate
          // bounces already-onboarded users back to the app if they type these URLs.
          {
            element: <OnboardingGate />,
            children: [
              {
                element: <OnboardingPageLayout />,
                children: [
                  { path: "/welcome", element: <WelcomePage /> },
                  { path: "/assessment", element: <AssessmentIntroPage /> },
                  { path: "/placement", element: <PlacementTestPage /> },
                  { path: "/placement/result", element: <PlacementResultPage /> },
                ],
              },
            ],
          },
          {
            element: <OnboardingPageLayout />,
            children: [
              { path: "/levels/exit-test", element: <ExitTestPage /> },
              { path: "/levels/exit-test/result", element: <ExitTestResultPage /> },
            ],
          },
          {
            // The app proper: requires a chosen level. A signed-in learner who has not yet
            // onboarded is bounced to /welcome before reaching any of these screens.
            element: <RequireOnboarded />,
            children: [
              {
                // Goal-based onboarding: once onboarded, a learner with no chosen goal is nudged into the
                // one-screen goal picker before the app renders (asked after the level-choice flow).
                element: <GoalGate />,
                children: [
                  {
                    element: <RequireMandatoryVocabularyReview />,
                    children: [
                      {
                        element: <ShellLayout />,
                        children: [
                          { path: "/home", element: <HomePage /> },
                          { path: "/levels", element: <LevelMapPage /> },
                          { path: "/app/speaking", element: <SpeakingPage /> },
                          { path: "/app/speaking/topics", element: <SpeakingTopicsPage /> },
                          { path: "/app/speaking/live-tutor/:tutorId", element: <LiveAccentTutorPage /> },
                          { path: "/app/speaking/free", element: <SpeakingPage /> },
                          {
                            path: "/app/speaking/topic/:topicId",
                            element: <SpeakingPage />,
                          },
                          {
                            path: "/app/speaking/free-talk",
                            element: <FreeTalkTopicsPage />,
                          },
                          {
                            path: "/app/speaking/free-talk/:topicCode",
                            element: <SpeakingPage />,
                          },
                          {
                            path: "/app/speaking/role-talk",
                            element: <RoleTalkScenariosPage />,
                          },
                          {
                            path: "/app/speaking/role-talk/:scenarioCode",
                            element: <SpeakingPage />,
                          },
                          // Legacy roleplay entry: the scenario catalog now has its own page. Redirect old links there.
                          {
                            path: "/app/speaking/roleplay",
                            element: (
                              <Navigate to="/app/speaking/role-talk" replace />
                            ),
                          },
                          {
                            path: "/app/speaking/pronunciation/:word",
                            element: <PronunciationDetailPage />,
                          },
                          {
                            path: "/app/speaking/practice-words",
                            element: <SpeakingPracticeWordsPage />,
                          },
                          {
                            path: "/pronounciation/:word",
                            element: <LegacyPronunciationRedirect />,
                          },
                          {
                            path: "/video",
                            element: <NativeCapabilityRoute path="/video"><VideoCatalogPage /></NativeCapabilityRoute>,
                          },
                          {
                            path: "/video/search",
                            element: <NativeCapabilityRoute path="/video/search"><VideoFullSearchPage /></NativeCapabilityRoute>,
                          },
                          {
                            path: "/video/playlists/:playlistId/:episodeNumber",
                            element: <NativeCapabilityRoute path="/video/playlists"><VideoPlaylistPlayerPage /></NativeCapabilityRoute>,
                          },
                          {
                            path: "/video/playlists/:playlistId",
                            element: <NativeCapabilityRoute path="/video/playlists"><VideoPlaylistPage /></NativeCapabilityRoute>,
                          },
                          {
                            path: "/video/playlist/:playlistId",
                            element: <NativeCapabilityRoute path="/video/playlist"><VideoPlaylistPage /></NativeCapabilityRoute>,
                          },
                          {
                            path: "/video/:id/play",
                            element: <NativeCapabilityRoute path="/video/play"><VideoPlayerPage /></NativeCapabilityRoute>,
                          },
                          {
                            path: "/video/:id/quiz",
                            element: <NativeCapabilityRoute path="/video/quiz"><VideoQuizPage /></NativeCapabilityRoute>,
                          },
                          { path: "/reading", element: <ReadingCatalogPage /> },

                          { path: "/books", element: <BooksCatalogPage /> },
                          {
                            path: "/books/:bookId",
                            element: <BookDetailPage />,
                          },
                          {
                            path: "/books/:bookId/sections/:sectionId",
                            element: <BookSectionPage />,
                          },
                          { path: "/app/grammar", element: <GrammarCatalogPage /> },
                          {
                            path: "/app/grammar/topic/:topicId",
                            element: <GrammarLessonPage />,
                          },
                          {
                            path: "/listening/topic/:topicId",
                            element: <ListeningTopicPage />,
                          },
                          {
                            path: "/reading/topic/:topicId",
                            element: <ReadingTopicPage />,
                          },
                          {
                            path: "/writing/topic/:topicId",
                            element: <WritingTaskPage />,
                          },
                          { path: "/writing", element: <WritingCatalogPage /> },
                          {
                            path: "/writing/task/:topicId",
                            element: <WritingTaskPage />,
                          },

                          {
                            path: "/listening",
                            element: <ListeningCatalogPage />,
                          },

                          {
                            path: "/notifications",
                            element: (
                              <Navigate to="/home?notifications=open" replace />
                            ),
                          },
                          {
                            path: "/app/vocabulary",
                            element: (
                              <Navigate to="/app/vocabulary/topics" replace />
                            ),
                          },
                          {
                            path: "/app/vocabulary/topics",
                            element: <VocabularyTopicsPage />,
                          },
                          {
                            path: "/app/vocabulary/topic/:topicId",
                            element: <VocabularyTopicPage />,
                          },
                          {
                            path: "/app/vocabulary/topic/:topicId/pronunciation/:wordIndex",
                            element: <VocabularyPronunciationPage />,
                          },
                          {
                            path: "/app/vocabulary/review",
                            element: <VocabularyReviewPage />,
                          },
                          {
                            path: "/app/vocabulary/saved",
                            element: <MySavedWordsPage />,
                          },
                          {
                            path: "/app/vocabulary/saved/:topicId/practice",
                            element: <SavedVocabularyTopicPracticePage />,
                          },
                          { path: "/progress", element: <ProgressPage /> },
                          // Leaderboard lives INSIDE the app shell so it shares the same left
                          // Sidebar (Reyting) as /home - same jungle 3D design language.
                          {
                            path: "/leaderboard",
                            element: <LeaderboardPage />,
                          },
                          {
                            path: "/leaderboard/:learnerId",
                            element: <LeaderboardLearnerPage />,
                          },
                          { path: "/profile", element: <ProfilePage /> },
                          { path: "/support", element: <SupportChatPage /> },
                        ],
                      },
                    ],
                  },
                ],
              },
            ],
          },
          // Admin routes intentionally live outside ShellLayout: learner navigation must never
          // render in the operator workspace. RequireAdmin and AdminAreaLayout provide the only
          // chrome for every /admin route.
          {
            element: <RequireAdmin />,
            children: [
              {
                element: <AdminAreaLayout />,
                children: [
                  { path: "/admin", element: <AdminPage /> },
                  { path: "/admin/curriculum", element: <AdminCurriculumPage /> },
                  { path: "/admin/users", element: <AdminUsersPage /> },
                  { path: "/admin/support", element: <AdminSupportPage /> },
                  {
                    path: "/admin/users/:userId",
                    element: <AdminUserDetailPage />,
                  },
                  {
                    path: "/admin/vocabulary",
                    element: <AdminVocabularyPage />,
                  },
                  {
                    path: "/admin/vocabulary/:id/edit",
                    element: <AdminVocabularyEditPage />,
                  },
                  {
                    path: "/admin/vocabulary-images",
                    element: <AdminVocabularyImagesPage />,
                  },
                  { path: "/admin/grammar", element: <AdminGrammarPage /> },
                  {
                    path: "/admin/grammar/:id/edit",
                    element: <AdminGrammarEditPage />,
                  },
                  { path: "/admin/listening", element: <AdminListeningPage /> },
                  {
                    path: "/admin/listening/:id/edit",
                    element: <AdminListeningEditPage />,
                  },
                  { path: "/admin/reading", element: <AdminReadingPage /> },
                  {
                    path: "/admin/reading/:id/edit",
                    element: <AdminReadingEditPage />,
                  },
                  {
                    path: "/admin/metrics",
                    element: (
                      <Suspense fallback={<LoadingSkeleton variant="dashboard" />}>
                        <FounderDashboardPage />
                      </Suspense>
                    ),
                  },
                  {
                    element: <RequireSuperAdmin />,
                    children: [
                      {
                        path: "/admin/notifications",
                        element: <AdminNotificationsPage />,
                      },
                      {
                        path: "/admin/sections",
                        element: <AdminSectionsPage />,
                      },
                      {
                        path: "/admin/server",
                        element: (
                          <Suspense fallback={<LoadingSkeleton variant="dashboard" />}>
                            <ServerHealthPage />
                          </Suspense>
                        ),
                      },
                    ],
                  },
                ],
              },
            ],
          },
            ],
          },
        ],
      },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
  ],
  },
]);
