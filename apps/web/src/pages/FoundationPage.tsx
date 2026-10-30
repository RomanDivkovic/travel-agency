import { useApiHealth } from '../hooks/useApiHealth';
import { sv } from '../content/sv';
import { Container } from '../components/ui/Container';

export function FoundationPage() {
  const status = useApiHealth();
  const messages = {
    checking: sv.diagnostics.checking,
    connected: sv.diagnostics.connected,
    unavailable: sv.diagnostics.unavailable,
  };

  return (
    <Container>
      <main>
        <h1>{sv.diagnostics.title}</h1>
        <p>{sv.diagnostics.description}</p>
        <p role="status">{messages[status]}</p>
      </main>
    </Container>
  );
}
