import { defineConfig } from '@playwright/test';
import path from 'node:path';
const directory = process.env.TRAVEL_E2E_DIRECTORY;
const connection = process.env.TEST_DATABASE_CONNECTION;
if (
  !directory ||
  process.env.TRAVEL_CUSTOMER_E2E !== '1' ||
  !connection ||
  !/Database=[^;]*_e2e_tests(?:;|$)/i.test(connection)
) {
  throw new Error(
    'Run the guarded CustomerBrowserJourney backend test with an isolated *_e2e_tests database.',
  );
}
export default defineConfig({
  testDir: './e2e',
  workers: 1,
  retries: 0,
  timeout: 180000,
  outputDir: path.join(directory, 'results'),
  reporter: [
    ['list'],
    ['html', { outputFolder: path.join(directory, 'report'), open: 'never' }],
  ],
  use: {
    baseURL: 'http://localhost:5177',
    browserName: 'chromium',
    actionTimeout: 10000,
    trace: 'off',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      command:
        'dotnet run --project services/api/TravelAgency.Api --no-launch-profile --urls http://localhost:5084',
      cwd: '../..',
      stdout: 'pipe',
      url: 'http://localhost:5084/api/health/ready',
      timeout: 120000,
      reuseExistingServer: false,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE: 'false',
        DOTNET_USE_POLLING_FILE_WATCHER: '1',
        ConnectionStrings__TravelAgency: connection,
        Cors__AllowedOrigins__0: 'http://localhost:5177',
        CustomerPortal__PublicOrigin: 'http://localhost:5177',
        Email__Provider: 'Capture',
        Email__WorkerEnabled: 'true',
        Email__PollSeconds: '1',
        Email__CaptureDirectory: path.join(directory, 'mail'),
        Logging__LogLevel__Default: 'Warning',
        Messaging__NotificationDelaySeconds: '0',
        Messaging__NotificationCooldownSeconds: '30',
        TravelDocuments__StorageRoot: process.env.TRAVEL_E2E_STORAGE_ROOT!,
        TravelDocuments__ScannerPort:
          process.env.TRAVEL_E2E_SCANNER_PORT ?? '3310',
      },
    },
    {
      command:
        'npm run build -- --outDir "$TRAVEL_E2E_BUILD_DIRECTORY" && npm run preview -- --port 5177 --outDir "$TRAVEL_E2E_BUILD_DIRECTORY"',
      stdout: 'pipe',
      url: 'http://localhost:5177',
      timeout: 120000,
      reuseExistingServer: false,
      env: {
        VITE_API_BASE_URL: 'http://localhost:5084',
        TRAVEL_E2E_BUILD_DIRECTORY: path.join(directory, 'bundle'),
        CHOKIDAR_USEPOLLING: 'true',
      },
    },
  ],
});
