"use client";

import { Suspense, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Sidebar } from "@/components/Sidebar";
import { useAuth } from "@/lib/auth";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { user, ready } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (ready && !user) router.replace("/login");
  }, [ready, user, router]);

  if (!ready || !user) {
    return <div className="p-10 text-sm text-slate-500">Carregando...</div>;
  }

  return (
    <div className="md:pl-56">
      <Sidebar />
      <main className="mx-auto max-w-6xl p-4 md:p-8">
        <Suspense fallback={<div className="text-sm text-slate-500">Carregando...</div>}>{children}</Suspense>
      </main>
    </div>
  );
}
