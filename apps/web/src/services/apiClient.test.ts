import { AxiosError } from 'axios';
import { expect, it } from 'vitest';
import { ApiError, apiClient } from './apiClient';

it('converts transport errors into a safe centralized error', async () => {
  const request = apiClient.get('/api/health', {
    adapter: () => Promise.reject(new AxiosError('Sensitive server detail')),
  });
  await expect(request).rejects.toBeInstanceOf(ApiError);
  await expect(request).rejects.toThrow(
    'API-anropet misslyckades. Försök igen senare.',
  );
});
