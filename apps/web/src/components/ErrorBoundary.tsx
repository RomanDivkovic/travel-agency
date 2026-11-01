import { Component, type ReactNode } from 'react';
import { useTranslations } from '../localization/useLanguage';
import type { Translations } from '../content/types';

class Boundary extends Component<
  { children: ReactNode; messages: Translations['errors'] },
  { hasError: boolean }
> {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  render() {
    const sv = { errors: this.props.messages };
    if (this.state.hasError) {
      return (
        <main>
          <h1>{sv.errors.title}</h1>
          <p role="alert">{sv.errors.description}</p>
          <button onClick={() => window.location.reload()}>
            {sv.errors.reload}
          </button>
        </main>
      );
    }
    return this.props.children;
  }
}

export function ErrorBoundary({ children }: { children: ReactNode }) {
  const t = useTranslations();
  return <Boundary messages={t.errors}>{children}</Boundary>;
}
