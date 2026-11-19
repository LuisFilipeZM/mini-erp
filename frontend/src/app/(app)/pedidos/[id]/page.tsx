"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, post } from "@/lib/api";
import { date, money } from "@/lib/format";
import type { Order } from "@/lib/types";
import { Alert, PageHeader } from "@/components/ui";
import { StatusBadge } from "../StatusBadge";

export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();

  const { data: order, isLoading, error } = useQuery({
    queryKey: ["orders", id],
    queryFn: () => api<Order>(`/api/orders/${id}`),
  });

  const action = useMutation({
    mutationFn: (kind: "confirm" | "cancel") => post(`/api/orders/${id}/${kind}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["products"] });
      queryClient.invalidateQueries({ queryKey: ["movements"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  if (isLoading) return <p className="text-sm text-slate-500">Carregando...</p>;
  if (error || !order) return <Alert>Pedido não encontrado.</Alert>;

  function cancel() {
    const msg =
      order!.status === "Confirmed"
        ? "Cancelar este pedido? Os itens voltarão para o estoque."
        : "Cancelar este pedido?";
    if (confirm(msg)) action.mutate("cancel");
  }

  return (
    <>
      <PageHeader title={`Pedido #${order.number}`} subtitle={`Criado em ${date(order.createdAt, true)} por ${order.sellerName}`}>
        <Link href="/pedidos" className="btn-secondary">
          Voltar
        </Link>
        {order.status !== "Cancelled" && (
          <button className="btn-secondary" onClick={cancel} disabled={action.isPending}>
            Cancelar pedido
          </button>
        )}
        {order.status === "Open" && (
          <button className="btn-primary" onClick={() => action.mutate("confirm")} disabled={action.isPending}>
            Confirmar e baixar estoque
          </button>
        )}
      </PageHeader>

      {action.error && (
        <div className="mb-4">
          <Alert>{action.error.message}</Alert>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="card space-y-3 p-5 text-sm">
          <div>
            <div className="text-slate-500">Status</div>
            <StatusBadge status={order.status} />
          </div>
          <div>
            <div className="text-slate-500">Cliente</div>
            <div className="font-medium">{order.customerName}</div>
            {order.customerDocument && <div className="text-slate-500">{order.customerDocument}</div>}
          </div>
          {order.confirmedAt && (
            <div>
              <div className="text-slate-500">Confirmado em</div>
              <div>{date(order.confirmedAt, true)}</div>
            </div>
          )}
          {order.cancelledAt && (
            <div>
              <div className="text-slate-500">Cancelado em</div>
              <div>{date(order.cancelledAt, true)}</div>
            </div>
          )}
          {order.notes && (
            <div>
              <div className="text-slate-500">Observações</div>
              <div className="whitespace-pre-line">{order.notes}</div>
            </div>
          )}
        </div>

        <div className="card overflow-x-auto lg:col-span-2">
          <table className="table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Produto</th>
                <th className="text-right!">Qtd.</th>
                <th className="text-right!">Unitário</th>
                <th className="text-right!">Total</th>
              </tr>
            </thead>
            <tbody>
              {order.items.map((i) => (
                <tr key={i.productId}>
                  <td className="font-mono text-xs">{i.sku}</td>
                  <td>{i.productName}</td>
                  <td className="text-right">{i.quantity}</td>
                  <td className="text-right">{money(i.unitPrice)}</td>
                  <td className="text-right">{money(i.total)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td colSpan={4} className="text-right font-medium">
                  Total
                </td>
                <td className="text-right text-base font-semibold">{money(order.total)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    </>
  );
}
