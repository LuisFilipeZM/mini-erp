"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { money } from "@/lib/format";
import { useDebounce } from "@/lib/hooks";
import { usePermissions } from "@/lib/auth";
import type { Paged, Product } from "@/lib/types";
import { Badge, Modal, PageHeader, Pagination, TableMessage } from "@/components/ui";
import { ProductForm } from "./ProductForm";

export default function ProductsPage() {
  const params = useSearchParams();
  const can = usePermissions();

  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [status, setStatus] = useState("true");
  const [belowMinimum, setBelowMinimum] = useState(params.get("belowMinimum") === "true");
  const [sort, setSort] = useState("name");
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<Product | "new" | null>(null);

  const debouncedSearch = useDebounce(search);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["products", { debouncedSearch, category, status, belowMinimum, sort, page }],
    queryFn: () =>
      api<Paged<Product>>("/api/products", {
        query: { search: debouncedSearch, category, active: status, belowMinimum: belowMinimum || null, sort, page },
      }),
    placeholderData: keepPreviousData,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["products", "categories"],
    queryFn: () => api<string[]>("/api/products/categories"),
  });

  // volta pra página 1 sempre que um filtro muda
  const filter = <T,>(setter: (v: T) => void) => (value: T) => {
    setter(value);
    setPage(1);
  };

  return (
    <>
      <PageHeader title="Produtos" subtitle="Cadastro e posição de estoque">
        {can.manageStock && (
          <button className="btn-primary" onClick={() => setEditing("new")}>
            Novo produto
          </button>
        )}
      </PageHeader>

      <div className="card mb-4 grid gap-3 p-4 sm:grid-cols-2 lg:grid-cols-5">
        <input
          className="input lg:col-span-2"
          placeholder="Buscar por nome ou SKU"
          value={search}
          onChange={(e) => filter(setSearch)(e.target.value)}
        />
        <select className="input" value={category} onChange={(e) => filter(setCategory)(e.target.value)}>
          <option value="">Todas as categorias</option>
          {categories.map((c) => (
            <option key={c}>{c}</option>
          ))}
        </select>
        <select className="input" value={status} onChange={(e) => filter(setStatus)(e.target.value)}>
          <option value="true">Ativos</option>
          <option value="false">Inativos</option>
          <option value="">Todos</option>
        </select>
        <select className="input" value={sort} onChange={(e) => setSort(e.target.value)}>
          <option value="name">Nome (A-Z)</option>
          <option value="name_desc">Nome (Z-A)</option>
          <option value="stock">Menor estoque</option>
          <option value="stock_desc">Maior estoque</option>
          <option value="price">Menor preço</option>
          <option value="price_desc">Maior preço</option>
          <option value="recent">Mais recentes</option>
        </select>
        <label className="flex items-center gap-2 text-sm text-slate-600 lg:col-span-5">
          <input type="checkbox" checked={belowMinimum} onChange={(e) => filter(setBelowMinimum)(e.target.checked)} />
          Somente abaixo do estoque mínimo
        </label>
      </div>

      <div className="card overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>SKU</th>
              <th>Produto</th>
              <th>Categoria</th>
              <th className="text-right!">Preço</th>
              <th className="text-right!">Custo médio</th>
              <th className="text-right!">Estoque</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading && <TableMessage colSpan={7}>Carregando...</TableMessage>}
            {isError && <TableMessage colSpan={7}>Erro ao carregar produtos.</TableMessage>}
            {data?.items.length === 0 && <TableMessage colSpan={7}>Nenhum produto encontrado.</TableMessage>}
            {data?.items.map((p) => (
              <tr key={p.id} className="hover:bg-slate-50">
                <td className="font-mono text-xs">{p.sku}</td>
                <td>
                  {p.name} {!p.active && <Badge>inativo</Badge>}
                </td>
                <td className="text-slate-500">{p.category ?? "-"}</td>
                <td className="text-right">{money(p.salePrice)}</td>
                <td className="text-right text-slate-500">{money(p.costPrice)}</td>
                <td className="text-right">
                  <span className={p.belowMinimum ? "font-semibold text-red-600" : ""}>
                    {p.stock} {p.unit}
                  </span>
                  <div className="text-xs text-slate-400">mín. {p.minStock}</div>
                </td>
                <td className="text-right">
                  {can.manageStock && (
                    <button className="text-sm text-teal-700 hover:underline" onClick={() => setEditing(p)}>
                      Editar
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onChange={setPage} />}
      </div>

      <Modal
        title={editing === "new" ? "Novo produto" : "Editar produto"}
        open={editing !== null}
        onClose={() => setEditing(null)}
        wide
      >
        {editing !== null && (
          <ProductForm
            key={editing === "new" ? "new" : editing.id}
            product={editing === "new" ? undefined : editing}
            categories={categories}
            onDone={() => setEditing(null)}
          />
        )}
      </Modal>
    </>
  );
}
