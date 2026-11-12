import { getSession, setSession } from "./session";

const API_URL =process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

type Query = Record<string, string | number | boolean | null | undefined>;

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public fieldErrors: Record<string, string> = {},
  ) {
    super(message);
  }
}

// "Items[0].Quantity" -> "items[0].quantity"
function camel(key: string) {
  return key
    .split(".")
    .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
    .join(".");
}

async function toError(res: Response) {
  try {
    const body = await res.json();
    const fields: Record<string, string> = {};
    for (const [key, messages] of Object.entries(body.errors ?? {})) {
      fields[camel(key)] = (messages as string[])[0];
    }
    const message = body.detail ?? (Object.keys(fields).length ? "Verifique os campos destacados." : body.title);
    return new ApiError(res.status, message ?? "Erro inesperado", fields);
  } catch {
    return new ApiError(res.status, `Erro ${res.status}`);
  }
}

export async function api<T>(path: string, init: RequestInit & { query?: Query } = {}): Promise<T> {
  const { query, ...options } = init;
  const url = new URL(path, API_URL);

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== "") url.searchParams.set(key, String(value));
    }
  }

  const token = getSession()?.token;
  const headers = new Headers(options.headers);
  if (options.body) headers.set("Content-Type", "application/json");
  if (token) headers.set("Authorization", `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(url, { ...options, headers });
  } catch {
    throw new ApiError(0, "Não foi possível conectar à API.");
  }

  // token expirado ou usuário desativado
  if (res.status === 401 && token) setSession(null);

  if (!res.ok) throw await toError(res);

  if (res.status === 204 || res.headers.get("content-length") === "0") return undefined as T;
  return res.json() as Promise<T>;
}

export const post = <T>(path: string, body?: unknown) =>
  api<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined });

export const put = <T>(path: string, body: unknown) => api<T>(path, { method: "PUT", body: JSON.stringify(body) });
