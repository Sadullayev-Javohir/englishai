namespace Domain.Common;

/// <summary>
/// Why a learner is studying English - asked once during onboarding (PROJECT-SPEC: goal-based
/// onboarding). Drives goal-tailored topic recommendations on the home/level-map/speaking surfaces
/// and unlocks the founder dashboard's per-goal segment metrics (which segment pays, retains, and
/// is worth targeting first). Stored on the learner's <c>LearnerProfile</c> (asked right after the
/// level-choice flow, once the profile exists). Kept in Domain.Common so both Learning (the profile
/// that stores it and the affinity table that reads it) and Identity can reference it without
/// cross-coupling.
///
/// The values are stored durably and are sent numerically by the SPA - keep this enum stable:
/// never renumber an existing value, only append new ones (same rule as ProductEventType).
/// </summary>
public enum LearningGoal
{
    /// <summary>Not chosen yet / skipped / a pre-feature account. The recommendation path falls
    /// back to the plain catalog order, so nothing regresses for these learners.</summary>
    Unspecified = 0,

    /// <summary>Preparing for IELTS / a CEFR certificate exam.</summary>
    IeltsCefr = 1,

    /// <summary>English for work / career.</summary>
    Work = 2,

    /// <summary>Migration / work-visa relocation.</summary>
    Migration = 3,

    /// <summary>Travel and tourism.</summary>
    Travel = 4,

    /// <summary>General conversational speaking.</summary>
    GeneralSpeaking = 5,

    /// <summary>School / university study.</summary>
    School = 6,
}
