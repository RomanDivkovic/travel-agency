import { Component, type ReactNode } from 'react';

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
          <h1>Något gick fel</h1>
          <p role="alert">Sidan kunde inte visas. Försök ladda om sidan.</p>
          <button onClick={() => window.location.reload()}>Ladda om</button>
        </main>
      );
    }
    return this.props.children;
  }
}
