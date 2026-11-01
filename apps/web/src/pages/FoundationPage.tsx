import { useApiHealth } from '../hooks/useApiHealth';
import { useTranslations } from '../localization/useLanguage';
import { Container } from '../components/ui/Container';

export function FoundationPage() {
  const t = useTranslations();
  const status = useApiHealth();
  const messages = {
    checking: t.diagnostics.checking,
    connected: t.diagnostics.connected,
    unavailable: t.diagnostics.unavailable,
  };

  return (
    <Container>
      <main>
        <h1>{t.diagnostics.title}</h1>
        <p>{t.diagnostics.description}</p>
        <p role="status">{messages[status]}</p>
      </main>
    </Container>
  );
}
