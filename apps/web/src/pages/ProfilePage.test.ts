import { describe, expect, it } from "vitest";
import { AdminRole } from "@/api/types";
import { canAccessDeveloperApi } from "@/lib/developerApiAccess";

describe("profile developer API visibility", () => {
  it("allows only the designated super-admin account", () => {
    expect(canAccessDeveloperApi("javohirsadullayev836@gmail.com", AdminRole.SuperAdmin)).toBe(true);
    expect(canAccessDeveloperApi("learner@example.com", AdminRole.SuperAdmin)).toBe(false);
    expect(canAccessDeveloperApi("javohirsadullayev836@gmail.com", AdminRole.Admin)).toBe(false);
    expect(canAccessDeveloperApi("javohirsadullayev836@gmail.com", AdminRole.None)).toBe(false);
  });
});