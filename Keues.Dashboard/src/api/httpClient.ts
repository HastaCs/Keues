export class ApiError extends Error {
  status: number;
  details: unknown;

  constructor(status: number, message: string, details?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
  }
}

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
}

function getBaseUrl() {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
  const normalizedBaseUrl = baseUrl.trim();
  return normalizedBaseUrl.endsWith('/') ? normalizedBaseUrl.slice(0, -1) : normalizedBaseUrl;
}

function buildUrl(path: string) {
  const baseUrl = getBaseUrl();
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  return `${baseUrl}${normalizedPath}`;
}

function parseJsonSafely(payload: string): unknown {
  try {
    return JSON.parse(payload);
  } catch {
    return payload;
  }
}

function buildErrorMessage(status: number, details: unknown) {
  if (typeof details === 'string' && details.trim().length > 0) {
    return details;
  }

  if (details && typeof details === 'object') {
    const maybeMessage = (details as { message?: unknown }).message;
    if (typeof maybeMessage === 'string' && maybeMessage.trim().length > 0) {
      return maybeMessage;
    }

    const maybeTitle = (details as { title?: unknown }).title;
    if (typeof maybeTitle === 'string' && maybeTitle.trim().length > 0) {
      return maybeTitle;
    }

    const maybeError = (details as { error?: unknown }).error;
    if (typeof maybeError === 'string' && maybeError.trim().length > 0) {
      return maybeError;
    }
  }

  return `Request failed with status ${status}`;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...restOptions } = options;
  let response: Response;

  try {
    response = await fetch(buildUrl(path), {
      ...restOptions,
      headers: {
        'Content-Type': 'application/json',
        ...headers,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (error) {
    throw new ApiError(
      0,
      'No se pudo conectar con la API. Revisa URL/CORS y que el servicio este activo.',
      error
    );
  }

  const rawPayload = await response.text();
  const parsedPayload = rawPayload.length > 0 ? parseJsonSafely(rawPayload) : undefined;

  if (!response.ok) {
    throw new ApiError(
      response.status,
      buildErrorMessage(response.status, parsedPayload),
      parsedPayload
    );
  }

  return parsedPayload as T;
}
