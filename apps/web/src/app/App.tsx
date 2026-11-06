const ManagementPage = lazy(() =>
  import('../features/business-management/ManagementPages').then((m) => ({
    default: m.ManagementPage,
  })),
);
const ContentEditorPage = lazy(() =>
  import('../features/business-management/ManagementPages').then((m) => ({
    default: m.ContentEditorPage,
  })),
);
const MediaLibraryPage = lazy(() =>
  import('../features/business-management/MediaLibraryPage').then((m) => ({
    default: m.MediaLibraryPage,
  })),
);
const ContentPreviewPage = lazy(() =>
  import('../features/business-management/ContentPreviewPage').then((m) => ({
    default: m.ContentPreviewPage,
  })),
);
import { PolicyPage } from '../features/business-management/PublishedComponents';
const MessagingInboxPage = lazy(() =>
  import('../features/messaging/InboxPage').then((m) => ({
    default: m.InboxPage,
  })),
);
const MessagingConversationPage = lazy(() =>
  import('../features/messaging/ConversationPage').then((m) => ({
    default: m.ConversationPage,
  })),
);
const NewConversationPage = lazy(() =>
  import('../features/messaging/NewConversationPage').then((m) => ({
    default: m.NewConversationPage,
  })),
);
const DocumentsPage = lazy(() =>
  import('../features/travel-documents/DocumentPages').then((m) => ({
    default: m.DocumentsPage,
  })),
);
const ItineraryPage = lazy(() =>
  import('../features/travel-documents/DocumentPages').then((m) => ({
    default: m.ItineraryPage,
  })),
);
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
const CustomerRoot = lazy(() =>
  import('../features/customer-portal/session').then((m) => ({
    default: m.CustomerRoot,
  })),
);
const RequireCustomer = lazy(() =>
  import('../features/customer-portal/session').then((m) => ({
    default: m.RequireCustomer,
  })),
);
const CustomerLayout = lazy(() =>
  import('../features/customer-portal/CustomerLayout').then((m) => ({
    default: m.CustomerLayout,
  })),
);
const CustomerAuthPage = lazy(() =>
  import('../features/customer-portal/AuthPages').then((m) => ({
    default: m.CustomerAuthPage,
  })),
);
const CustomerTokenPage = lazy(() =>
  import('../features/customer-portal/AuthPages').then((m) => ({
    default: m.CustomerTokenPage,
  })),
);
const CustomerDashboardPage = lazy(() =>
  import('../features/customer-portal/CustomerDashboardPage').then((m) => ({
    default: m.CustomerDashboardPage,
  })),
);
const CustomerListPage = lazy(() =>
  import('../features/customer-portal/CustomerListPage').then((m) => ({
    default: m.CustomerListPage,
  })),
);
const CustomerDetailPage = lazy(() =>
  import('../features/customer-portal/CustomerDetailPages').then((m) => ({
    default: m.CustomerDetailPage,
  })),
);
const CustomerProfilePage = lazy(() =>
  import('../features/customer-portal/CustomerSettingsPages').then((m) => ({
    default: m.CustomerProfilePage,
  })),
);
const CustomerSecurityPage = lazy(() =>
  import('../features/customer-portal/CustomerSettingsPages').then((m) => ({
    default: m.CustomerSecurityPage,
  })),
);
export function App() {
  return (
    <LanguageProvider>
      <ErrorBoundary>
        <RouteFocus />
        <Suspense fallback={<p role="status">Hämtar… / Loading…</p>}>
          <Routes>
            <Route element={<CustomerRoot />}>
              <Route
                path="/login"
                element={<CustomerAuthPage mode="login" />}
              />
              <Route
                path="/register"
                element={<CustomerAuthPage mode="register" />}
              />
              <Route
                path="/forgot-password"
                element={<CustomerAuthPage mode="forgot-password" />}
              />
              <Route
                path="/resend-verification"
                element={<CustomerAuthPage mode="resend-verification" />}
              />
              <Route
                path="/verify-email"
                element={<CustomerTokenPage action="verify" />}
              />
              <Route
                path="/reset-password"
                element={<CustomerTokenPage action="reset" />}
              />
              <Route
                path="/confirm-request"
                element={<CustomerTokenPage action="claim" />}
              />
              <Route element={<RequireCustomer />}>
                <Route path="/account" element={<CustomerLayout />}>
                  <Route index element={<CustomerDashboardPage />} />
                  <Route
                    path="requests"
                    element={
                      <CustomerListPage key="requests" kind="requests" />
                    }
                  />
                  <Route
                    path="quotations"
                    element={
                      <CustomerListPage key="quotations" kind="quotations" />
                    }
                  />
                  <Route
                    path="invoices"
                    element={
                      <CustomerListPage key="invoices" kind="invoices" />
                    }
                  />
                  <Route
                    path="proposals"
                    element={
                      <CustomerListPage key="proposals" kind="proposals" />
                    }
                  />
                  <Route
                    path="bookings"
                    element={
                      <CustomerListPage key="bookings" kind="bookings" />
                    }
                  />
                  <Route
                    path="requests/:id"
                    element={<CustomerDetailPage kind="requests" />}
                  />
                  <Route
                    path="quotations/:id"
                    element={<CustomerDetailPage kind="quotations" />}
                  />
                  <Route
                    path="invoices/:id"
                    element={<CustomerDetailPage kind="invoices" />}
                  />
                  <Route
                    path="proposals/:id"
                    element={<CustomerDetailPage kind="proposals" />}
                  />
                  <Route
                    path="bookings/:id"
                    element={<CustomerDetailPage kind="bookings" />}
                  />
                  <Route
                    path="bookings/:id/documents"
                    element={<DocumentsPage />}
                  />
                  <Route
                    path="bookings/:id/itinerary"
                    element={<ItineraryPage />}
                  />
                  <Route path="messages" element={<MessagingInboxPage />} />
                  <Route
                    path="messages/new"
                    element={<NewConversationPage />}
                  />
                  <Route
                    path="messages/:id"
                    element={<MessagingConversationPage />}
                  />
                  <Route path="profile" element={<CustomerProfilePage />} />
                  <Route path="security" element={<CustomerSecurityPage />} />
                </Route>
              </Route>
            </Route>

            <Route
              path="/admin"
              element={
                <AuthProvider>
                  <RequireAdmin />
                </AuthProvider>
              }
            >
              <Route
                path="content/preview/:key"
                element={<ContentPreviewPage />}
              />
              <Route element={<AdminLayout />}>
                <Route
                  path="settings/business"
                  element={<ContentEditorPage documentKey="business" />}
                />
                <Route
                  path="settings/preferences"
                  element={<ContentEditorPage documentKey="preferences" />}
                />
                <Route
                  path="settings/services"
                  element={<ManagementPage kind="services" />}
                />
                <Route path="content" element={<ManagementPage />} />
                <Route
                  path="content/faqs"
                  element={<ManagementPage kind="faqs" />}
                />
                <Route
                  path="content/policies"
                  element={<ManagementPage kind="policies" />}
                />
                <Route path="content/media" element={<MediaLibraryPage />} />
                <Route
                  path="content/edit/:key"
                  element={<ContentEditorPage />}
                />
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
                <Route
                  path="bookings/:id/documents"
                  element={<DocumentsPage admin />}
                />
                <Route
                  path="bookings/:id/itinerary"
                  element={<ItineraryPage admin />}
                />
                <Route path="proposals" element={<AdminProposalListPage />} />
                <Route path="proposals/:id" element={<AdminProposalPage />} />
                <Route
                  path="proposals/:id/edit"
                  element={<AdminProposalPage />}
                />
                <Route path="messages" element={<MessagingInboxPage admin />} />
                <Route
                  path="messages/:id"
                  element={<MessagingConversationPage admin />}
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
              <Route path="/policies/:type" element={<PolicyPage />} />
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
