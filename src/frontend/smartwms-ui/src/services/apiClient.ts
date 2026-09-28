import { fetchAuthSession } from 'aws-amplify/auth'

export type QueryValue = string | number | boolean | null | undefined;
export type QueryParams = Record<string, QueryValue>;

const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "/api";
const apiBaseUrl = rawApiBaseUrl.endsWith("/") ? rawApiBaseUrl.slice(0, -1) : rawApiBaseUrl;

function toQueryString(queryParams?: QueryParams): string {
  if (!queryParams) {
    return "";
  }

  const searchParams = new URLSearchParams();

  Object.entries(queryParams).forEach(([key, value]) => {
    if (value === null || value === undefined || value === "") {
      return;
    }

    searchParams.set(key, String(value));
  });

  const query = searchParams.toString();
  return query ? `?${query}` : "";
}

export function buildApiPath(path: string, queryParams?: QueryParams): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${normalizedPath}${toQueryString(queryParams)}`;
}

function getErrorMessage(payload: unknown, fallbackMessage: string): string {
  if (typeof payload === "string" && payload.trim()) {
    return payload;
  }

  if (payload && typeof payload === "object") {
    const maybeProblem = payload as {
      title?: unknown;
      detail?: unknown;
      message?: unknown;
    };

    if (typeof maybeProblem.detail === "string" && maybeProblem.detail.trim()) {
      return maybeProblem.detail;
    }

    if (typeof maybeProblem.message === "string" && maybeProblem.message.trim()) {
      return maybeProblem.message;
    }

    if (typeof maybeProblem.title === "string" && maybeProblem.title.trim()) {
      return maybeProblem.title;
    }
  }

  return fallbackMessage;
}

export async function fetchJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  try {
    const session = await fetchAuthSession()
    const accessToken = session.tokens?.accessToken?.toString()
    if (accessToken && !headers.has("Authorization")) {
      headers.set("Authorization", `Bearer ${accessToken}`)
    }
  } catch {
    // not logged in — request proceeds without token
    // protected endpoints will return 401
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers
  });


  if (!response.ok) {
    const fallbackMessage = `Request failed with status ${response.status}`;

    try {
      const contentType = response.headers.get("content-type") ?? "";

      if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
        const payload = (await response.json()) as unknown;
        throw new Error(getErrorMessage(payload, fallbackMessage));
      }

      const textPayload = await response.text();
      throw new Error(getErrorMessage(textPayload, fallbackMessage));
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }

      throw new Error(fallbackMessage);
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
