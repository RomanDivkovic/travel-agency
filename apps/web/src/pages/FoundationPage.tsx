import { useApiHealth } from '../hooks/useApiHealth';

export function FoundationPage() {
  const status = useApiHealth();
  const messages = {
    checking: 'Kontrollerar API…',
    connected: 'API anslutet',
    unavailable: 'API är inte tillgängligt',
  };

  return (
    <>
      <h1>Resebyråns applikation körs</h1>
      <p>
        Teknisk grund för Phase 0. Den publika webbplatsen byggs i nästa fas.
      </p>
      <p role="status">{messages[status]}</p>
    </>
  );
}
