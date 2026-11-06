import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router';
import { useApiResource } from '../../hooks/useApiResource';
import { cmsApi } from './api';
import {
  WebsiteContext,
  PreviewContext,
  PreviewPageContext,
} from './contentContext';
import { useCmsCopy } from './copy';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { Header } from '../../components/layout/Header';
import { Footer } from '../../components/layout/Footer';
import { HomePage } from '../home/HomePage';
import { AboutPage } from '../public/AboutPage';
import { ServicesPage } from '../public/ServicesPage';
import { PricingPage } from '../public/PricingPage';
import { ContactPage } from '../public/ContactPage';
import { FaqPage } from '../public/FaqPage';
import { PolicyPreview } from './PolicyPreview';
import styles from './Management.module.scss';
export function ContentPreviewPage() {
  const { key = '' } = useParams();
  const t = useCmsCopy();
  const load = useCallback((s: AbortSignal) => cmsApi.preview(key, s), [key]);
  const state = useApiResource(key, load);
  const [width, setWidth] = useState('1440');
  const page = key.startsWith('service:')
    ? 'services'
    : key === 'business'
      ? 'contact'
      : key.startsWith('faq:')
        ? 'faq'
        : key.replace('page:', '');
  return (
    <>
      <div className={styles.panel}>
        <p role="status">{t.privatePreview}</p>
        <Link to={'/admin/content/edit/' + encodeURIComponent(key)}>
          {t.back}
        </Link>
        <LanguageSwitcher />
        <label>
          {t.preview}
          <select value={width} onChange={(e) => setWidth(e.target.value)}>
            {[375, 768, 1024, 1440].map((v) => (
              <option key={v}>{v}</option>
            ))}
          </select>
        </label>
      </div>
      {state.error !== undefined ? (
        <p role="alert">{t.error}</p>
      ) : !state.data ? (
        <p role="status">{t.loading}</p>
      ) : (
        <PreviewPageContext value={page}>
          <PreviewContext value={true}>
            <WebsiteContext value={state.data}>
              <div className={styles.preview} style={{ width: Number(width) }}>
                <Header />
                <main id="main-content">
                  {page === 'home' || page === 'footer' ? (
                    <HomePage />
                  ) : page === 'about' ? (
                    <AboutPage />
                  ) : page === 'services' ? (
                    <ServicesPage />
                  ) : page === 'pricing' ? (
                    <PricingPage />
                  ) : page === 'contact' ? (
                    <ContactPage />
                  ) : page === 'faq' ? (
                    <FaqPage />
                  ) : (
                    <PolicyPreview documentKey={key} />
                  )}
                </main>
                <Footer />
              </div>
            </WebsiteContext>
          </PreviewContext>
        </PreviewPageContext>
      )}
    </>
  );
}
