"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError, post } from "@/lib/api";
import { money } from "@/lib/format";
import { usePermissions } from "@/lib/auth";
import type { Product } from "@/lib/types";
import { Alert, Field, NoPermission, PageHeader } from "@/components/ui";
import { ProductPicker } from "@/components/ProductPicker";

type Line = { product: Product; quantity: number };

export default function NewOrderPage() {
  const can = usePermissions();
  const router = useRouter();
  const queryClient = useQueryClient();

  const [customerName, setCustomerName] = useState("");
  const [customerDocument, setCustomerDocument] = useState("");
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<Line[]>([]);

  const total = lines.reduce((sum, l) => sum + l.quantity * l.product.salePrice, 0);

  const mutation = useMutation({
    mutationFn: (confirm: boolean) =>
      post<{ id: string }>("/api/orders", {
        customerName,
        customerDocument: customerDocument || null,
        notes: notes || null,
        items: lines.map((l) => ({ productId: l.product.id, quantity: l.quantity })),
        confirm,
      }),
    onSuccess: (order) => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["products"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      router.push(`/pedidos/${order.id}`);
    },
  });

  if (!can.sell) return <NoPermission />;

  const errors = mutation.error instanceof ApiError ? mutation.error.fieldErrors : {};

  function addProduct(product: Product) {
    setLines((current) => {
      const existing = current.find((l) => l.product.id === product.id);
      if (existing) {
        return current.map((l) => (l === existing ? { ...l, quantity: l.quantity + 1 } : l));
      }
      return [...current, { product, quantity: 1 }];
    });
  }

  function updateQuantity(id: string, value: string) {
    const quantity = Math.max(0, Math.trunc(Number(value) || 0));
    setLines((current) => current.map((l) => (l.product.id === id ? { ...l, quantity } : l)));
  }

  return (
    <>
      <PageHeader title="Novo pedido de venda">
        <Link href="/pedidos" className="btn-secondary">
          Voltar
        </Link>
      </PageHeader>

      {mutation.error && (
        <div className="mb-4">
          <Alert>{mutation.error.message}</Alert>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="card space-y-4 p-5">
          <h2 className="font-semibold">Cliente</h2>
          <Field label="Nome / razão social" error={errors.customerName}>
            <input className="input" value={customerName} onChange={(e) => setCustomerName(e.target.value)} />
          </Field>
          <Field label="CPF / CNPJ" error={errors.customerDocument}>
            <input className="input" value={customerDocument} onChange={(e) => setCustomerDocument(e.target.value)} />
          </Field>
          <Field label="Observações" error={errors.notes}>
            <textarea className="input" rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} />
          </Field>
        </div>

        <div className="card p-5 lg:col-span-2">
          <h2 className="mb-3 font-semibold">Itens</h2>
          <ProductPicker onSelect={addProduct} />
          {errors.items && <p className="mt-2 text-xs text-red-600">{errors.items}</p>}

          <table className="table mt-4">
            <thead>
              <tr>
                <th>Produto</th>
                <th className="w-24">Qtd.</th>
                <th className="text-right!">Unitário</th>
                <th className="text-right!">Subtotal</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {lines.length === 0 && (
                <tr>
                  <td colSpan={5} className="py-8 text-center text-slate-500">
                    Busque e adicione produtos acima.
                  </td>
                </tr>
              )}
              {lines.map((l) => (
                <tr key={l.product.id}>
                  <td>
                    {l.product.name}
                    <div className={`text-xs ${l.quantity > l.product.stock ? "text-red-600" : "text-slate-400"}`}>
                      disponível: {l.product.stock}
                    </div>
                  </td>
                  <td>
                    <input
                      className="input py-1"
                      inputMode="numeric"
                      value={l.quantity}
                      onChange={(e) => updateQuantity(l.product.id, e.target.value)}
                    />
                  </td>
                  <td className="text-right">{money(l.product.salePrice)}</td>
                  <td className="text-right">{money(l.quantity * l.product.salePrice)}</td>
                  <td className="text-right">
                    <button
                      className="text-slate-400 hover:text-red-600"
                      onClick={() => setLines((c) => c.filter((x) => x.product.id !== l.product.id))}
                      aria-label="Remover"
                    >
                      ×
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="mt-4 flex items-center justify-between border-t border-slate-200 pt-4">
            <div className="text-lg">
              Total: <span className="font-semibold">{money(total)}</span>
            </div>
            <div className="flex gap-2">
              <button className="btn-secondary" disabled={mutation.isPending || lines.length === 0} onClick={() => mutation.mutate(false)}>
                Salvar em aberto
              </button>
              <button className="btn-primary" disabled={mutation.isPending || lines.length === 0} onClick={() => mutation.mutate(true)}>
                Salvar e confirmar
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
