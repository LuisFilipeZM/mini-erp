"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { date, money } from "@/lib/format";
import { useDebounce } from "@/lib/hooks";
import { usePermissions } from "@/lib/auth";
import type { OrderSummary, Paged } from "@/lib/types";
import { NoPermission, PageHeader, Pagination, TableMessage } from "@/components/ui";
import { StatusBadge } from "./StatusBadge";

export default function OrdersPage() {
  const can = usePermissions();
  const router = useRouter();
  const params = useSearchParams();

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState(params.get("status") ?? "");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebounce(search);

  const { data, isLoading } = useQuery({
    queryKey: ["orders", { debouncedSearch, status, from, to, page }],
    queryFn: () =>
      api<Paged<OrderSummary>>("/api/orders", { query: { search: debouncedSearch, status, from, to, page } }),
    placeholderData: keepPreviousData,
    enabled: can.sell,
  });

  if (!can.sell) return <NoPermission />;

  return (
    <>
      <PageHeader title="Pedidos de venda">
        <Link href="/pedidos/novo" className="btn-primary">
          Novo pedido
        </Link>
      </PageHeader>

      <div className="card mb-4 grid gap-3 p-4 sm:grid-cols-2 lg:grid-cols-4">
        <input
          className="input"
          placeholder="Nº do pedido ou cliente"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
        />
        <select className="input" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
          <option value="">Todos os status</option>
          <option value="Open">Em aberto</option>
          <option value="Confirmed">Confirmados</option>
          <option value="Cancelled">Cancelados</option>
        </select>
        <input type="date" className="input" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} title="De" />
        <input type="date" className="input" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} title="Até" />
      </div>

      <div className="card overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Nº</th>
              <th>Cliente</th>
              <th>Data</th>
              <th>Vendedor</th>
              <th className="text-right!">Itens</th>
              <th className="text-right!">Total</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && <TableMessage colSpan={7}>Carregando...</TableMessage>}
            {data?.items.length === 0 && <TableMessage colSpan={7}>Nenhum pedido encontrado.</TableMessage>}
            {data?.items.map((o) => (
              <tr key={o.id} className="cursor-pointer hover:bg-slate-50" onClick={() => router.push(`/pedidos/${o.id}`)}>
                <td className="font-medium">#{o.number}</td>
                <td>{o.customerName}</td>
                <td className="text-slate-500">{date(o.createdAt)}</td>
                <td className="text-slate-500">{o.sellerName}</td>
                <td className="text-right">{o.itemCount}</td>
                <td className="text-right font-medium">{money(o.total)}</td>
                <td>
                  <StatusBadge status={o.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onChange={setPage} />}
      </div>
    </>
  );
}
