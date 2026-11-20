"use client";

import { useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, ApiError, post, put } from "@/lib/api";
import { date, roleLabel } from "@/lib/format";
import { useDebounce } from "@/lib/hooks";
import { usePermissions } from "@/lib/auth";
import type { Paged, Role, User } from "@/lib/types";
import { Alert, Badge, Field, Modal, NoPermission, PageHeader, Pagination, TableMessage } from "@/components/ui";

function UserForm({ user, onDone }: { user?: User; onDone: () => void }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [role, setRole] = useState<Role>(user?.role ?? "Seller");
  const [active, setActive] = useState(user?.active ?? true);
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      user
        ? put(`/api/users/${user.id}`, { name, role, active, password: password || null })
        : post("/api/users", { name, email, role, password }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
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
      <Field label="Nome" error={errors.name}>
        <input className="input" value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>
      <Field label="E-mail" error={errors.email}>
        <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} disabled={!!user} required />
      </Field>
      <div className="grid grid-cols-2 gap-3">
        <Field label="Perfil" error={errors.role}>
          <select className="input" value={role} onChange={(e) => setRole(e.target.value as Role)}>
            {Object.entries(roleLabel).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </Field>
        <Field label={user ? "Nova senha (opcional)" : "Senha"} error={errors.password}>
          <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required={!user} />
        </Field>
      </div>
      {user && (
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={active} onChange={(e) => setActive(e.target.checked)} /> Usuário ativo
        </label>
      )}
      <div className="flex justify-end gap-2">
        <button type="button" className="btn-secondary" onClick={onDone}>
          Cancelar
        </button>
        <button className="btn-primary" disabled={mutation.isPending}>
          Salvar
        </button>
      </div>
    </form>
  );
}

export default function UsersPage() {
  const can = usePermissions();
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<User | "new" | null>(null);
  const debouncedSearch = useDebounce(search);

  const { data, isLoading } = useQuery({
    queryKey: ["users", { debouncedSearch, role, page }],
    queryFn: () => api<Paged<User>>("/api/users", { query: { search: debouncedSearch, role, page } }),
    placeholderData: keepPreviousData,
    enabled: can.admin,
  });

  if (!can.admin) return <NoPermission />;

  return (
    <>
      <PageHeader title="Usuários">
        <button className="btn-primary" onClick={() => setEditing("new")}>
          Novo usuário
        </button>
      </PageHeader>

      <div className="card mb-4 grid gap-3 p-4 sm:grid-cols-3">
        <input
          className="input sm:col-span-2"
          placeholder="Nome ou e-mail"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
        />
        <select className="input" value={role} onChange={(e) => { setRole(e.target.value); setPage(1); }}>
          <option value="">Todos os perfis</option>
          {Object.entries(roleLabel).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      <div className="card overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>Perfil</th>
              <th>Criado em</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading && <TableMessage colSpan={5}>Carregando...</TableMessage>}
            {data?.items.map((u) => (
              <tr key={u.id}>
                <td>
                  {u.name} {!u.active && <Badge color="red">inativo</Badge>}
                </td>
                <td className="text-slate-500">{u.email}</td>
                <td>
                  <Badge color="blue">{roleLabel[u.role]}</Badge>
                </td>
                <td className="text-slate-500">{date(u.createdAt)}</td>
                <td className="text-right">
                  <button className="text-sm text-teal-700 hover:underline" onClick={() => setEditing(u)}>
                    Editar
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {data && <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onChange={setPage} />}
      </div>

      <Modal title={editing === "new" ? "Novo usuário" : "Editar usuário"} open={editing !== null} onClose={() => setEditing(null)}>
        {editing !== null && (
          <UserForm
            key={editing === "new" ? "new" : editing.id}
            user={editing === "new" ? undefined : editing}
            onDone={() => setEditing(null)}
          />
        )}
      </Modal>
    </>
  );
}
