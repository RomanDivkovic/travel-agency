import { Navigate, Route, Routes } from 'react-router';
import { AppLayout } from '../layouts/AppLayout';
import { FoundationPage } from '../pages/FoundationPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { HomePage } from '../features/home/HomePage';
import { WizardPage } from '../features/travel-request/WizardPage';
import { routes } from './config/routes';
import { RouteFocus } from './RouteFocus';
import { ErrorBoundary } from '../components/ErrorBoundary';
import { LanguageProvider } from '../localization/LanguageProvider';
import { ServicesPage } from '../features/public/ServicesPage';
import { PricingPage } from '../features/public/PricingPage';
import { AboutPage } from '../features/public/AboutPage';
import { ContactPage } from '../features/public/ContactPage';
import { FaqPage } from '../features/public/FaqPage';

export function App() {
  return (
    <LanguageProvider>
      <ErrorBoundary>
        <RouteFocus />
        <Routes>
          <Route element={<AppLayout />}>
            <Route index element={<HomePage />} />
            <Route path={routes.services} element={<ServicesPage />} />
            <Route path={routes.pricing} element={<PricingPage />} />
            <Route path={routes.about} element={<AboutPage />} />
            <Route path={routes.contact} element={<ContactPage />} />
            <Route path={routes.faq} element={<FaqPage />} />
            <Route path={routes.planTrip} element={<WizardPage />} />
            <Route
              path={routes.aboutPreview}
              element={<Navigate to={routes.about} replace />}
            />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
          {import.meta.env.DEV ? (
            <Route path={routes.diagnostics} element={<FoundationPage />} />
          ) : null}
        </Routes>
      </ErrorBoundary>
    </LanguageProvider>
  );
}
