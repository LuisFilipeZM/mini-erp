"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { money } from "@/lib/format";
import { useDebounce } from "@/lib/hooks";
import type { Paged, Product } from "@/lib/types";

export function ProductPicker({ onSelect, placeholder }: { onSelect: (p: Product) => void; placeholder?: string }) {
  const [term, setTerm] = useState("");
  const [open, setOpen] = useState(false);
  const search = useDebounce(term, 250);

  const { data, isFetching } = useQuery({
    queryKey: ["products", "picker", search],
    queryFn: () => api<Paged<Product>>("/api/products", { query: { search, active: true, pageSize: 8 } }),
    enabled: open,
  });

  return (
    <div className="relative">
      <input
        className="input"
        value={term}
        placeholder={placeholder ?? "Buscar produto por nome ou SKU..."}
        onChange={(e) => {
          setTerm(e.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
      />
      {open && (
        <ul className="absolute z-20 mt-1 max-h-72 w-full overflow-y-auto rounded-md border border-slate-200 bg-white shadow-lg">
          {isFetching && !data && <li className="px-3 py-2 text-sm text-slate-500">Buscando...</li>}
          {data?.items.length === 0 && <li className="px-3 py-2 text-sm text-slate-500">Nenhum produto encontrado</li>}
          {data?.items.map((p) => (
            <li key={p.id}>
              <button
                type="button"
                className="flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-slate-50"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => {
                  onSelect(p);
                  setTerm("");
                  setOpen(false);
                }}
              >
                <span>
                  <span className="font-mono text-xs text-slate-500">{p.sku}</span> {p.name}
                </span>
                <span className="text-xs text-slate-500">
                  {money(p.salePrice)} · estoque {p.stock}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
