const currency = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const number = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 });

export const money = (value: number) => currency.format(value);
export const num = (value: number) => number.format(value);

export function date(value: string | null | undefined, withTime = false) {
  if (!value) return "-";
  const d = new Date(value);
  return withTime
    ? d.toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })
    : d.toLocaleDateString("pt-BR");
}

// yyyy-mm-dd no fuso local, para inputs type="date"
export function isoDate(d: Date) {
  const offset = d.getTimezoneOffset() * 60000;
  return new Date(d.getTime() - offset).toISOString().slice(0, 10);
}

export const roleLabel: Record<string, string> = {
  Admin: "Administrador",
  Seller: "Vendedor",
  StockKeeper: "Estoquista",
};

export const statusLabel: Record<string, string> = {
  Open: "Em aberto",
  Confirmed: "Confirmado",
  Cancelled: "Cancelado",
};

export const originLabel: Record<string, string> = {
  Manual: "Manual",
  Sale: "Venda",
  SaleCancellation: "Estorno de venda",
};
