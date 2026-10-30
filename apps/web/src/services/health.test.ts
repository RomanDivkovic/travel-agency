import { afterEach, expect, it, vi } from 'vitest';
import { ApiError, apiClient } from './apiClient';
import { getApiHealth } from './health';

afterEach(() => vi.restoreAllMocks());

it('accepts a healthy response and forwards cancellation', async () => {
  const signal = new AbortController().signal;
  const get = vi
    .spyOn(apiClient, 'get')
    .mockResolvedValue({ data: { status: 'healthy' } });
  await expect(getApiHealth(signal)).resolves.toBeUndefined();
  expect(get).toHaveBeenCalledWith('/api/health', { signal });
});

it.each([null, '<html>SPA fallback</html>', {}, { status: 'unhealthy' }])(
  'rejects an invalid health response: %j',
  async (data) => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data });
    await expect(getApiHealth()).rejects.toBeInstanceOf(ApiError);
  },
);
