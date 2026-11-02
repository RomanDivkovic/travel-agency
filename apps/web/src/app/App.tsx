const RequestDetailPage = lazy(() =>
  import('../features/admin/RequestDetailPage').then((m) => ({
    default: m.RequestDetailPage,
  })),
);
import { lazy, Suspense } from 'react';
const AdminResearchPage = lazy(() =>
  import('../features/travel-research/AdminResearchPage').then((m) => ({
    default: m.AdminResearchPage,
  })),
);
const AdminProposalListPage = lazy(() =>
  import('../features/travel-research/AdminProposalListPage').then((m) => ({
    default: m.AdminProposalListPage,
  })),
);
const AdminProposalPage = lazy(() =>
  import('../features/travel-research/AdminProposalPage').then((m) => ({
    default: m.AdminProposalPage,
  })),
);
const CustomerProposalPage = lazy(() =>
  import('../features/travel-research/CustomerProposalPage').then((m) => ({
    default: m.CustomerProposalPage,
  })),
);
const AdminInvoiceListPage = lazy(() =>
  import('../features/payments/AdminInvoiceListPage').then((m) => ({
    default: m.AdminInvoiceListPage,
  })),
);
const AdminInvoiceDetailPage = lazy(() =>
  import('../features/payments/AdminInvoiceDetailPage').then((m) => ({
    default: m.AdminInvoiceDetailPage,
  })),
);
const CustomerInvoicePage = lazy(() =>
  import('../features/payments/CustomerInvoicePage').then((m) => ({
    default: m.CustomerInvoicePage,
  })),
);
const SandboxPaymentPage = lazy(() =>
  import('../features/payments/SandboxPaymentPage').then((m) => ({
    default: m.SandboxPaymentPage,
  })),
);
import { Navigate, Route, Routes } from 'react-router';
import { AdminQuotationListPage } from '../features/quotations/AdminQuotationListPage';
import { AdminQuotationDetailPage } from '../features/quotations/AdminQuotationDetailPage';
import { CustomerQuotationPage } from '../features/quotations/CustomerQuotationPage';
import { AuthProvider, RequireAdmin } from '../features/admin/AuthProvider';
import { AdminLayout } from '../features/admin/AdminLayout';
import { LoginPage } from '../features/admin/LoginPage';
import { DashboardPage } from '../features/admin/DashboardPage';
import { RequestsPage } from '../features/admin/RequestsPage';
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

const AdminBookingListPage = lazy(() =>
  import('../features/bookings/BookingList').then((m) => ({
    default: m.AdminBookingListPage,
  })),
);
const AdminBookingDetailPage = lazy(() =>
  import('../features/bookings/BookingWorkspace').then((m) => ({
    default: m.AdminBookingDetailPage,
  })),
);
export function App() {
  return (
    <LanguageProvider>
      <ErrorBoundary>
        <RouteFocus />
        <Suspense fallback={<p role="status">Hämtar… / Loading…</p>}>
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
                <Route
                  path="requests/:id/research"
                  element={<AdminResearchPage />}
                />
                <Route path="bookings" element={<AdminBookingListPage />} />
                <Route
                  path="bookings/:id"
                  element={<AdminBookingDetailPage />}
                />
                <Route path="proposals" element={<AdminProposalListPage />} />
                <Route path="proposals/:id" element={<AdminProposalPage />} />
                <Route
                  path="proposals/:id/edit"
                  element={<AdminProposalPage />}
                />
                <Route path="requests" element={<RequestsPage />} />
                <Route path="invoices" element={<AdminInvoiceListPage />} />
                <Route
                  path="invoices/:id"
                  element={<AdminInvoiceDetailPage />}
                />
                <Route path="quotations" element={<AdminQuotationListPage />} />
                <Route
                  path="quotations/:id"
                  element={<AdminQuotationDetailPage />}
                />
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
            <Route path="/invoice" element={<CustomerInvoicePage />} />
            <Route
              path="/payment/result"
              element={<CustomerInvoicePage result />}
            />
            {import.meta.env.DEV && (
              <Route path="/payment/sandbox" element={<SandboxPaymentPage />} />
            )}
            <Route path="/travel-proposal" element={<CustomerProposalPage />} />
            <Route path="/quote" element={<CustomerQuotationPage />} />
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
        </Suspense>
      </ErrorBoundary>
    </LanguageProvider>
  );
}
