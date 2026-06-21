import { AdminRole } from "@/api/types";

export function canAccessDeveloperApi(email?: string | null, role?: AdminRole): boolean {
  return email?.trim().toLowerCase() === "javohirsadullayev836@gmail.com" && role === AdminRole.SuperAdmin;
}