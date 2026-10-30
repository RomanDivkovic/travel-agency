import { Component, type ReactNode } from 'react';
import { sv } from '../content/sv';

export class ErrorBoundary extends Component<
  { children: ReactNode },
  { hasError: boolean }
> {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  render() {
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
