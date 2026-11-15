"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth, usePermissions } from "@/lib/auth";
import { roleLabel } from "@/lib/format";

export function Sidebar() {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const can = usePermissions();

  const links = [
    { href: "/", label: "Dashboard", show: true },
    { href: "/produtos", label: "Produtos", show: true },
    { href: "/estoque", label: "Movimentações", show: can.manageStock },
    { href: "/pedidos", label: "Pedidos de venda", show: can.sell },
    { href: "/relatorios/giro", label: "Giro de estoque", show: can.viewReports },
    { href: "/usuarios", label: "Usuários", show: can.admin },
  ];

  const isActive = (href: string) => (href === "/" ? pathname === "/" : pathname.startsWith(href));

  return (
    <aside className="flex w-full flex-col bg-slate-900 text-slate-300 md:fixed md:inset-y-0 md:left-0 md:w-56">
      <div className="px-5 py-5 text-lg font-semibold text-white">
        Mini<span className="text-teal-400">ERP</span>
      </div>
      <nav className="flex flex-1 flex-row flex-wrap gap-1 px-3 md:flex-col">
        {links
          .filter((l) => l.show)
          .map((l) => (
            <Link
              key={l.href}
              href={l.href}
              className={`rounded-md px-3 py-2 text-sm ${
                isActive(l.href) ? "bg-slate-800 text-white" : "hover:bg-slate-800/60 hover:text-white"
              }`}
            >
              {l.label}
            </Link>
          ))}
      </nav>
      {user && (
        <div className="border-t border-slate-800 px-5 py-4 text-sm">
          <div className="font-medium text-white">{user.name}</div>
          <div className="text-xs text-slate-400">{roleLabel[user.role]}</div>
          <button onClick={logout} className="mt-2 text-xs text-teal-400 hover:underline">
            Sair
          </button>
        </div>
      )}
    </aside>
  );
}
