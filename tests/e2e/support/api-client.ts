import { APIRequestContext } from '@playwright/test';

type HttpMethod = 'get' | 'post' | 'put' | 'delete';

export async function authJson<T>(
  request: APIRequestContext,
  method: HttpMethod,
  url: string,
  bearerToken: string,
  data?: unknown
): Promise<T> {
  const response = await request.fetch(url, {
    method,
    headers: {
      Authorization: `Bearer ${bearerToken}`,
      'Content-Type': 'application/json'
    },
    data
  });

  const text = await response.text();
  let body: unknown = {};

  if (text) {
    try {
      body = JSON.parse(text);
    } catch {
      body = text;
    }
  }

  if (!response.ok()) {
    throw new Error(
      `Request failed: ${method.toUpperCase()} ${url} -> ${response.status()} ${response.statusText()} | ${JSON.stringify(body)}`
    );
  }

  return body as T;
}

export function unwrapResult<T>(payload: unknown): T {
  if (payload && typeof payload === 'object' && 'value' in payload) {
    return (payload as { value: T }).value;
  }

  return payload as T;
}
