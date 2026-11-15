"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth";
import { Alert, Field } from "@/components/ui";

const demoUsers = [
  { label: "Admin", email: "admin@minierp.dev", password: "Admin@123" },
  { label: "Vendedor", email: "vendedor@minierp.dev", password: "Vendedor@123" },
  { label: "Estoquista", email: "estoque@minierp.dev", password: "Estoque@123" },
];

export default function LoginPage() {
  const { login, user, ready } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (ready && user) router.replace("/");
  }, [ready, user, router]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      router.replace("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha no login");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-900 p-4">
      <div className="w-full max-w-sm">
        <h1 className="mb-6 text-center text-2xl font-semibold text-white">
          Mini<span className="text-teal-400">ERP</span>
        </h1>
        <form onSubmit={handleSubmit} className="card space-y-4 p-6">
          {error && <Alert>{error}</Alert>}
          <Field label="E-mail">
            <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </Field>
          <Field label="Senha">
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </Field>
          <button className="btn-primary w-full" disabled={loading}>
            {loading ? "Entrando..." : "Entrar"}
          </button>
        </form>

        <div className="mt-4 text-center text-xs text-slate-400">
          <p className="mb-2">Usuários de demonstração:</p>
          <div className="flex justify-center gap-2">
            {demoUsers.map((u) => (
              <button
                key={u.email}
                type="button"
                className="rounded border border-slate-700 px-2 py-1 hover:bg-slate-800"
                onClick={() => {
                  setEmail(u.email);
                  setPassword(u.password);
                }}
              >
                {u.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
