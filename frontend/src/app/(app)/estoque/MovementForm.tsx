"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError, post } from "@/lib/api";
import { money } from "@/lib/format";
import type { MovementType, Product } from "@/lib/types";
import { Alert, Field } from "@/components/ui";
import { ProductPicker } from "@/components/ProductPicker";

export function MovementForm({ onDone }: { onDone: () => void }) {
  const queryClient = useQueryClient();
  const [product, setProduct] = useState<Product | null>(null);
  const [type, setType] = useState<MovementType>("In");
  const [quantity, setQuantity] = useState("");
  const [unitCost, setUnitCost] = useState("");
  const [note, setNote] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      post("/api/stock/movements", {
        productId: product?.id,
        type,
        quantity: Number(quantity),
        unitCost: type === "In" ? Number(unitCost.replace(",", ".")) : null,
        note: note || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["movements"] });
      queryClient.invalidateQueries({ queryKey: ["products"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      onDone();
    },
  });

  const errors = mutation.error instanceof ApiError ? mutation.error.fieldErrors : {};

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        mutation.mutate();
      }}
    >
      {mutation.error && <Alert>{mutation.error.message}</Alert>}

      <Field label="Produto" error={errors.productId}>
        {product ? (
          <div className="flex items-center justify-between rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm">
            <span>
              <span className="font-mono text-xs text-slate-500">{product.sku}</span> {product.name}
              <span className="ml-2 text-slate-500">
                (estoque: {product.stock}, custo: {money(product.costPrice)})
              </span>
            </span>
            <button type="button" className="text-teal-700 hover:underline" onClick={() => setProduct(null)}>
              trocar
            </button>
          </div>
        ) : (
          <ProductPicker
            onSelect={(p) => {
              setProduct(p);
              setUnitCost(String(p.costPrice));
            }}
          />
        )}
      </Field>

      <div className="flex gap-4 text-sm">
        <label className="flex items-center gap-2">
          <input type="radio" checked={type === "In"} onChange={() => setType("In")} /> Entrada
        </label>
        <label className="flex items-center gap-2">
          <input type="radio" checked={type === "Out"} onChange={() => setType("Out")} /> Saída
        </label>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <Field label="Quantidade" error={errors.quantity}>
          <input className="input" inputMode="numeric" value={quantity} onChange={(e) => setQuantity(e.target.value)} required />
        </Field>
        {type === "In" && (
          <Field label="Custo unitário" error={errors.unitCost}>
            <input className="input" inputMode="decimal" value={unitCost} onChange={(e) => setUnitCost(e.target.value)} />
          </Field>
        )}
      </div>

      <Field label="Observação" error={errors.note}>
        <input
          className="input"
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder={type === "In" ? "Ex.: NF 1234 - Fornecedor X" : "Ex.: avaria, uso interno"}
        />
      </Field>

      <div className="flex justify-end gap-2">
        <button type="button" className="btn-secondary" onClick={onDone}>
          Cancelar
        </button>
        <button className="btn-primary" disabled={!product || mutation.isPending}>
          Registrar
        </button>
      </div>
    </form>
  );
}
