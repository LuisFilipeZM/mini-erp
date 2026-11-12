import type { SessionUser } from "./types";

export type Session = { token: string; expiresAt: string; user: SessionUser };

const STORAGE_KEY = "minierp.session";
const listeners = new Set<() => void>();
let current: Session | null | undefined;

function read(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as Session;
    if (new Date(session.expiresAt) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function getSession() {
  if (current === undefined) current = read();
  return current;
}

export function setSession(session: Session | null) {
  current = session;
  if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  else localStorage.removeItem(STORAGE_KEY);
  listeners.forEach((l) => l());
}

export function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}
