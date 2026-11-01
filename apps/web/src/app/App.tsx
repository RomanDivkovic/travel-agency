import { Navigate, Route, Routes } from 'react-router';
import { AuthProvider, RequireAdmin } from '../features/admin/AuthProvider';
import { AdminLayout } from '../features/admin/AdminLayout';
import { LoginPage } from '../features/admin/LoginPage';
import { DashboardPage } from '../features/admin/DashboardPage';
import { RequestsPage } from '../features/admin/RequestsPage';
import { RequestDetailPage } from '../features/admin/RequestDetailPage';
import { AppLayout } from '../layouts/AppLayout';
import { FoundationPage } from '../pages/FoundationPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { HomePage } from '../features/home/HomePage';
import { SuccessPage } from '../features/travel-request/SuccessPage';
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
          <Route
            path="/admin"
            element={
              <AuthProvider>
                <RequireAdmin />
              </AuthProvider>
            }
          >
            <Route element={<AdminLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="requests" element={<RequestsPage />} />
              <Route path="requests/:id" element={<RequestDetailPage />} />
            </Route>
          </Route>
          <Route
            path="/admin/login"
            element={
              <AuthProvider>
                <LoginPage />
              </AuthProvider>
            }
          />
          <Route element={<AppLayout />}>
            <Route index element={<HomePage />} />
            <Route path={routes.services} element={<ServicesPage />} />
            <Route path={routes.pricing} element={<PricingPage />} />
            <Route path={routes.about} element={<AboutPage />} />
            <Route path={routes.contact} element={<ContactPage />} />
            <Route path={routes.faq} element={<FaqPage />} />
            <Route path="/plan-your-trip/success" element={<SuccessPage />} />
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
