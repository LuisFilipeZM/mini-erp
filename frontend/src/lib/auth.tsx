"use client";

import { createContext, useCallback, useContext, useMemo, useSyncExternalStore } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { post } from "./api";
import { getSession, setSession, subscribe, type Session } from "./session";
import type { Role, SessionUser } from "./types";

type AuthContextValue = {
  user: SessionUser | null;
  ready: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient();
  // no servidor o snapshot é undefined, ou seja, "ainda não sabemos"
  const session = useSyncExternalStore(subscribe, getSession, () => undefined);

  const logout = useCallback(() => {
    setSession(null);
    queryClient.clear();
  }, [queryClient]);

  const login = useCallback(async (email: string, password: string) => {
    setSession(await post<Session>("/api/auth/login", { email, password }));
  }, []);

  const value = useMemo(
    () => ({ user: session?.user ?? null, ready: session !== undefined, login, logout }),
    [session, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth precisa estar dentro de AuthProvider");
  return ctx;
}

export function usePermissions() {
  const { user } = useAuth();
  const is = (...roles: Role[]) => !!user && roles.includes(user.role);

  return {
    admin: is("Admin"),
    manageStock: is("Admin", "StockKeeper"),
    sell: is("Admin", "Seller"),
    viewReports: is("Admin", "StockKeeper"),
  };
}
