"use client";

import { useState } from "react";
import Link from "next/link";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { date, money, originLabel } from "@/lib/format";
import { usePermissions } from "@/lib/auth";
import type { Movement, Paged, Product } from "@/lib/types";
import { Badge, Modal, NoPermission, PageHeader, Pagination, TableMessage } from "@/components/ui";
import { ProductPicker } from "@/components/ProductPicker";
import { MovementForm } from "./MovementForm";

export default function StockPage() {
  const can = usePermissions();
  const [product, setProduct] = useState<Product | null>(null);
  const [type, setType] = useState("");
  const [origin, setOrigin] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["movements", { productId: product?.id, type, origin, from, to, page }],
    queryFn: () =>
      api<Paged<Movement>>("/api/stock/movements", {
        query: { productId: product?.id, type, origin, from, to, page },
      }),
    placeholderData: keepPreviousData,
    enabled: can.manageStock,
  });

  if (!can.manageStock) return <NoPermission />;

  return (
    <>
      <PageHeader title="Movimentações de estoque" subtitle="Entradas, saídas e baixas por venda">
        <button className="btn-primary" onClick={() => setShowForm(true)}>
          Nova movimentação
        </button>
      </PageHeader>

      <div className="card mb-4 grid gap-3 p-4 sm:grid-cols-2 lg:grid-cols-6">
        <div className="lg:col-span-2">
          {product ? (
            <div className="flex h-full items-center justify-between rounded-md border border-slate-300 px-3 text-sm">
              <span className="truncate">{product.name}</span>
              <button
                className="ml-2 text-slate-400 hover:text-slate-700"
                onClick={() => {
                  setProduct(null);
                  setPage(1);
                }}
              >
                ×
              </button>
            </div>
          ) : (
            <ProductPicker
              placeholder="Filtrar por produto..."
              onSelect={(p) => {
                setProduct(p);
                setPage(1);
              }}
            />
          )}
        </div>
        <select className="input" value={type} onChange={(e) => { setType(e.target.value); setPage(1); }}>
          <option value="">Entradas e saídas</option>
          <option value="In">Entradas</option>
          <option value="Out">Saídas</option>
        </select>
        <select className="input" value={origin} onChange={(e) => { setOrigin(e.target.value); setPage(1); }}>
          <option value="">Todas as origens</option>
          <option value="Manual">Manual</option>
          <option value="Sale">Venda</option>
          <option value="SaleCancellation">Estorno de venda</option>
        </select>
        <input type="date" className="input" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} title="De" />
        <input type="date" className="input" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} title="Até" />
      </div>

      <div className="card overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Produto</th>
              <th>Tipo</th>
              <th>Origem</th>
              <th className="text-right!">Qtd.</th>
              <th className="text-right!">Custo un.</th>
              <th className="text-right!">Saldo</th>
              <th>Usuário</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && <TableMessage colSpan={8}>Carregando...</TableMessage>}
            {data?.items.length === 0 && <TableMessage colSpan={8}>Nenhuma movimentação no filtro.</TableMessage>}
            {data?.items.map((m) => (
              <tr key={m.id}>
                <td className="whitespace-nowrap text-slate-500">{date(m.createdAt, true)}</td>
                <td>
                  <span className="font-mono text-xs text-slate-500">{m.sku}</span> {m.productName}
                  {m.note && <div className="text-xs text-slate-400">{m.note}</div>}
                </td>
                <td>{m.type === "In" ? <Badge color="green">Entrada</Badge> : <Badge color="red">Saída</Badge>}</td>
                <td>
                  {m.salesOrderId && can.sell ? (
                    <Link href={`/pedidos/${m.salesOrderId}`} className="text-teal-700 hover:underline">
                      {originLabel[m.origin]}
                    </Link>
                  ) : (
                    originLabel[m.origin]
                  )}
                </td>
                <td className="text-right">{m.type === "In" ? `+${m.quantity}` : `-${m.quantity}`}</td>
                <td className="text-right text-slate-500">{money(m.unitCost)}</td>
                <td className="text-right font-medium">{m.balanceAfter}</td>
                <td className="text-slate-500">{m.userName}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onChange={setPage} />}
      </div>

      <Modal title="Nova movimentação" open={showForm} onClose={() => setShowForm(false)}>
        {showForm && <MovementForm onDone={() => setShowForm(false)} />}
      </Modal>
    </>
  );
}
