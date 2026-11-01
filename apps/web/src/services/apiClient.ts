import axios from 'axios';

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly details?: unknown,
    public readonly retryAfter?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || undefined,
  timeout: 5000,
  headers: { Accept: 'application/json' },
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isCancel(error)) return Promise.reject(error);
    const status = axios.isAxiosError(error)
      ? error.response?.status
      : undefined;
    return Promise.reject(
      new ApiError(
        'API-anropet misslyckades. Försök igen senare.',
        status,
        axios.isAxiosError(error) ? error.response?.data : undefined,
        axios.isAxiosError(error)
          ? error.response?.headers['retry-after']
          : undefined,
      ),
    );
  },
);
