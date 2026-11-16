"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError, post, put } from "@/lib/api";
import type { Product } from "@/lib/types";
import { Alert, Field } from "@/components/ui";

type FormState = {
  sku: string;
  name: string;
  description: string;
  category: string;
  unit: string;
  salePrice: string;
  costPrice: string;
  minStock: string;
  initialStock: string;
  active: boolean;
};

function initial(product?: Product): FormState {
  return {
    sku: product?.sku ?? "",
    name: product?.name ?? "",
    description: product?.description ?? "",
    category: product?.category ?? "",
    unit: product?.unit ?? "UN",
    salePrice: product ? String(product.salePrice) : "",
    costPrice: product ? String(product.costPrice) : "",
    minStock: product ? String(product.minStock) : "0",
    initialStock: "0",
    active: product?.active ?? true,
  };
}

const toNumber = (v: string) => Number(v.replace(",", ".")) || 0;

export function ProductForm({ product, categories, onDone }: {
  product?: Product;
  categories: string[];
  onDone: () => void;
}) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState(() => initial(product));
  const editing = !!product;

  const set = (key: keyof FormState) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  const mutation = useMutation({
    mutationFn: () => {
      const base = {
        sku: form.sku,
        name: form.name,
        description: form.description || null,
        category: form.category || null,
        unit: form.unit,
        salePrice: toNumber(form.salePrice),
        minStock: Math.trunc(toNumber(form.minStock)),
      };
      return editing
        ? put(`/api/products/${product.id}`, { ...base, active: form.active })
        : post("/api/products", {
            ...base,
            costPrice: toNumber(form.costPrice),
            initialStock: Math.trunc(toNumber(form.initialStock)),
          });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["products"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      onDone();
    },
  });

  const errors = mutation.error instanceof ApiError ? mutation.error.fieldErrors : {};

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        mutation.mutate();
      }}
      className="space-y-4"
    >
      {mutation.error && <Alert>{mutation.error.message}</Alert>}

      <div className="grid grid-cols-3 gap-3">
        <Field label="SKU" error={errors.sku}>
          <input className="input uppercase" value={form.sku} onChange={set("sku")} required />
        </Field>
        <Field label="Nome" error={errors.name} className="col-span-2">
          <input className="input" value={form.name} onChange={set("name")} required />
        </Field>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <Field label="Categoria" error={errors.category} className="col-span-2">
          <input className="input" list="categories" value={form.category} onChange={set("category")} />
          <datalist id="categories">
            {categories.map((c) => (
              <option key={c} value={c} />
            ))}
          </datalist>
        </Field>
        <Field label="Unidade" error={errors.unit}>
          <input className="input uppercase" value={form.unit} onChange={set("unit")} maxLength={6} />
        </Field>
      </div>

      <Field label="Descrição" error={errors.description}>
        <textarea className="input" rows={2} value={form.description} onChange={set("description")} />
      </Field>

      <div className="grid grid-cols-3 gap-3">
        <Field label="Preço de venda" error={errors.salePrice}>
          <input className="input" inputMode="decimal" value={form.salePrice} onChange={set("salePrice")} required />
        </Field>
        <Field label={editing ? "Custo médio" : "Custo unitário"} error={errors.costPrice}>
          <input className="input" inputMode="decimal" value={form.costPrice} onChange={set("costPrice")} disabled={editing} />
        </Field>
        <Field label="Estoque mínimo" error={errors.minStock}>
          <input className="input" inputMode="numeric" value={form.minStock} onChange={set("minStock")} />
        </Field>
      </div>

      {editing ? (
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.active} onChange={(e) => setForm((f) => ({ ...f, active: e.target.checked }))} />
          Produto ativo
        </label>
      ) : (
        <Field label="Estoque inicial" error={errors.initialStock}>
          <input className="input w-40" inputMode="numeric" value={form.initialStock} onChange={set("initialStock")} />
        </Field>
      )}

      {editing && (
        <p className="text-xs text-slate-500">
          O custo é recalculado automaticamente pela média ponderada a cada entrada de estoque.
        </p>
      )}

      <div className="flex justify-end gap-2 pt-2">
        <button type="button" className="btn-secondary" onClick={onDone}>
          Cancelar
        </button>
        <button className="btn-primary" disabled={mutation.isPending}>
          {mutation.isPending ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
