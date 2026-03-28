export type E2EConfig = {
  apiBaseUrl: string;
  bearerToken?: string;
  organizationId?: string;
};

export function readConfig(): E2EConfig {
  return {
    apiBaseUrl: process.env.API_BASE_URL ?? 'https://localhost:7090',
    bearerToken: process.env.JOBFLOW_API_BEARER_TOKEN,
    organizationId: process.env.JOBFLOW_ORGANIZATION_ID
  };
}

export function requireBearerToken(): string {
  const token = process.env.JOBFLOW_API_BEARER_TOKEN;
  if (!token) {
    throw new Error(
      'JOBFLOW_API_BEARER_TOKEN is required for authenticated business-flow tests. ' +
      'Set it from a seeded account login token in local env or GitHub Actions secrets.'
    );
  }

  return token;
}

export function hasBearerToken(): boolean {
  return Boolean(process.env.JOBFLOW_API_BEARER_TOKEN);
}
