"use client";

import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { date, isoDate, money, num } from "@/lib/format";
import { useDebounce } from "@/lib/hooks";
import { usePermissions } from "@/lib/auth";
import type { TurnoverReport } from "@/lib/types";
import { NoPermission, PageHeader, Pagination, TableMessage } from "@/components/ui";

function daysAgo(n: number) {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return isoDate(d);
}

const presets = [
  { label: "7 dias", days: 6 },
  { label: "30 dias", days: 29 },
  { label: "60 dias", days: 59 },
  { label: "90 dias", days: 89 },
];

export default function TurnoverPage() {
  const can = usePermissions();
  const [from, setFrom] = useState(daysAgo(29));
  const [to, setTo] = useState(daysAgo(0));
  const [category, setCategory] = useState("");
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState("");
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebounce(search);

  const { data, isLoading, error } = useQuery({
    queryKey: ["reports", "turnover", { from, to, category, debouncedSearch, sort, page }],
    queryFn: () =>
      api<TurnoverReport>("/api/reports/stock-turnover", {
        query: { from, to, category, search: debouncedSearch, sort, page },
      }),
    placeholderData: keepPreviousData,
    enabled: can.viewReports,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["products", "categories"],
    queryFn: () => api<string[]>("/api/products/categories"),
  });

  if (!can.viewReports) return <NoPermission />;

  const rows = data?.rows;

  return (
    <>
      <PageHeader
        title="Giro de estoque"
        subtitle="Giro = quantidade vendida no período ÷ estoque médio ((inicial + final) / 2)"
      />

      <div className="card mb-4 space-y-3 p-4">
        <div className="flex flex-wrap items-center gap-2">
          {presets.map((p) => (
            <button
              key={p.days}
              className={`rounded-full border px-3 py-1 text-xs ${
                from === daysAgo(p.days) && to === daysAgo(0)
                  ? "border-teal-700 bg-teal-700 text-white"
                  : "border-slate-300 hover:bg-slate-100"
              }`}
              onClick={() => {
                setFrom(daysAgo(p.days));
                setTo(daysAgo(0));
                setPage(1);
              }}
            >
              {p.label}
            </button>
          ))}
        </div>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
          <input type="date" className="input" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
          <input type="date" className="input" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
          <select className="input" value={category} onChange={(e) => { setCategory(e.target.value); setPage(1); }}>
            <option value="">Todas as categorias</option>
            {categories.map((c) => (
              <option key={c}>{c}</option>
            ))}
          </select>
          <input
            className="input"
            placeholder="Produto ou SKU"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
          <select className="input" value={sort} onChange={(e) => setSort(e.target.value)}>
            <option value="">Maior giro</option>
            <option value="turnover">Menor giro</option>
            <option value="sold_desc">Mais vendidos</option>
            <option value="cogs_desc">Maior CMV</option>
            <option value="name">Nome</option>
          </select>
        </div>
      </div>

      {error && <p className="mb-4 text-sm text-red-600">{error.message}</p>}

      {data && (
        <div className="mb-4 grid grid-cols-2 gap-4 lg:grid-cols-3">
          <div className="card p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Período</div>
            <div className="mt-1 font-semibold">
              {date(data.from + "T12:00")} a {date(data.to + "T12:00")}
            </div>
            <div className="text-xs text-slate-500">{data.days} dias</div>
          </div>
          <div className="card p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">CMV no período</div>
            <div className="mt-1 text-xl font-semibold">{money(data.totalCostOfGoodsSold)}</div>
          </div>
          <div className="card p-4">
            <div className="text-xs uppercase tracking-wide text-slate-500">Giro geral (em valor)</div>
            <div className="mt-1 text-xl font-semibold">{num(data.overallTurnover)}x</div>
          </div>
        </div>
      )}

      <div className="card overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Produto</th>
              <th className="text-right!">Est. inicial</th>
              <th className="text-right!">Est. final</th>
              <th className="text-right!">Est. médio</th>
              <th className="text-right!">Vendido</th>
              <th className="text-right!">CMV</th>
              <th className="text-right!">Giro</th>
              <th className="text-right!">Cobertura</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && <TableMessage colSpan={8}>Calculando...</TableMessage>}
            {rows?.items.length === 0 && <TableMessage colSpan={8}>Sem dados para o período.</TableMessage>}
            {rows?.items.map((r) => (
              <tr key={r.productId}>
                <td>
                  <span className="font-mono text-xs text-slate-500">{r.sku}</span> {r.name}
                  {r.category && <div className="text-xs text-slate-400">{r.category}</div>}
                </td>
                <td className="text-right">{r.openingStock}</td>
                <td className="text-right">{r.closingStock}</td>
                <td className="text-right">{num(r.averageStock)}</td>
                <td className="text-right">{r.quantitySold}</td>
                <td className="text-right">{money(r.costOfGoodsSold)}</td>
                <td className="text-right font-semibold">{num(r.turnover)}x</td>
                <td className="text-right text-slate-500">
                  {r.coverageDays === null ? "-" : `${num(r.coverageDays)} dias`}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {rows && <Pagination page={rows.page} totalPages={rows.totalPages} totalCount={rows.totalCount} onChange={setPage} />}
      </div>

      <p className="mt-3 text-xs text-slate-500">
        Cobertura: quantos dias o estoque final dura mantendo a média de vendas do período.
      </p>
    </>
  );
}
