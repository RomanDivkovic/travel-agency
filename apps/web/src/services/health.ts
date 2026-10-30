import { ApiError, apiClient } from './apiClient';

export async function getApiHealth(signal?: AbortSignal): Promise<void> {
  const { data } = await apiClient.get<unknown>('/api/health', { signal });
  if (
    typeof data !== 'object' ||
    data === null ||
    !('status' in data) ||
    data.status !== 'healthy'
  ) {
    throw new ApiError('API returnerade ett oväntat hälsosvar.');
  }
}
