import { Route, Routes } from 'react-router';
import { AppLayout } from '../layouts/AppLayout';
import { FoundationPage } from '../pages/FoundationPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { HomePage } from '../features/home/HomePage';
import { ComingSoonPage } from '../pages/ComingSoonPage';
import { routes } from './config/routes';
import { RouteFocus } from './RouteFocus';

export function App() {
  return (
    <>
      <RouteFocus />
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<HomePage />} />
          <Route path={routes.planTrip} element={<ComingSoonPage />} />
          <Route
            path={routes.aboutPreview}
            element={<ComingSoonPage about />}
          />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
        {import.meta.env.DEV ? (
          <Route path={routes.diagnostics} element={<FoundationPage />} />
        ) : null}
      </Routes>
    </>
  );
}
