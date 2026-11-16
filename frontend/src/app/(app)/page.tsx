"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { money, num } from "@/lib/format";
import type { Dashboard } from "@/lib/types";
import { useAuth } from "@/lib/auth";
import { PageHeader } from "@/components/ui";

function Stat({ label, value, hint, href }: { label: string; value: string; hint?: string; href?: string }) {
  const content = (
    <div className="card h-full p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</div>
      <div className="mt-1 text-2xl font-semibold text-slate-900">{value}</div>
      {hint && <div className="mt-1 text-xs text-slate-500">{hint}</div>}
    </div>
  );
  return href ? <Link href={href} className="hover:opacity-80">{content}</Link> : content;
}

export default function DashboardPage() {
  const { user } = useAuth();
  const { data, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => api<Dashboard>("/api/dashboard"),
  });

  if (isLoading || !data) return <p className="text-sm text-slate-500">Carregando...</p>;

  const max = Math.max(...data.last14Days.map((d) => d.total), 1);

  return (
    <>
      <PageHeader title={`Olá, ${user?.name.split(" ")[0]}`} subtitle="Resumo da operação" />

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Stat label="Vendas no mês" value={money(data.salesThisMonth)} hint={`${data.ordersThisMonth} pedidos confirmados`} />
        <Stat label="Pedidos em aberto" value={String(data.openOrders)} href="/pedidos?status=Open" />
        <Stat label="Produtos ativos" value={String(data.activeProducts)} href="/produtos" />
        <Stat label="Valor em estoque" value={money(data.stockValue)} hint="a preço de custo" />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <h2 className="mb-4 font-semibold">Vendas dos últimos 14 dias</h2>
          <div className="flex h-48 gap-1.5">
            {data.last14Days.map((d) => (
              <div key={d.date} className="group relative flex h-full flex-1 flex-col items-center">
                <div className="flex w-full flex-1 items-end">
                  <div
                    className="w-full rounded-t bg-teal-600 transition group-hover:bg-teal-700"
                    style={{ height: `${(d.total / max) * 100}%`, minHeight: d.total > 0 ? 4 : 0 }}
                  />
                </div>
                <span className="mt-1 text-[10px] text-slate-400">{d.date.slice(8, 10)}</span>
                <div className="pointer-events-none absolute -top-8 hidden whitespace-nowrap rounded bg-slate-800 px-2 py-1 text-xs text-white group-hover:block">
                  {money(d.total)} · {d.orders} ped.
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="card p-5">
          <h2 className="mb-3 font-semibold">
            Estoque baixo <span className="text-sm font-normal text-slate-500">({num(data.belowMinimum)})</span>
          </h2>
          {data.lowStock.length === 0 ? (
            <p className="text-sm text-slate-500">Nenhum produto abaixo do mínimo.</p>
          ) : (
            <ul className="divide-y divide-slate-100 text-sm">
              {data.lowStock.map((p) => (
                <li key={p.id} className="flex justify-between py-2">
                  <span className="truncate pr-2">{p.name}</span>
                  <span className="whitespace-nowrap text-red-600">
                    {p.stock} / {p.minStock}
                  </span>
                </li>
              ))}
            </ul>
          )}
          <Link href="/produtos?belowMinimum=true" className="mt-3 inline-block text-sm text-teal-700 hover:underline">
            Ver todos
          </Link>
        </div>
      </div>
    </>
  );
}
