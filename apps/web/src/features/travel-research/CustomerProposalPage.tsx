import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { BrandLogo } from '../../components/layout/BrandLogo';
import { LanguageSwitcher } from '../../components/layout/LanguageSwitcher';
import { useApiResource } from '../../hooks/useApiResource';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { useLanguage } from '../../localization/useLanguage';
import { ApiError } from '../../services/apiClient';
import { publicProposalApi } from './api';
import { useResearchCopy } from './copy';
import { ProposalDecision } from './ProposalDecision';
import { ProposalSummary } from './ProposalSummary';
import { ResearchError } from './ResearchShared';
import type { CustomerProposal } from './types';
import styles from './Research.module.scss';
function CustomerView({ token }: { token: string }) {
  const t = useResearchCopy();
  const { setLanguage } = useLanguage();
  usePageMetadata(t.customerTitle);
  const [revision, setRevision] = useState(0);
  const [confirmed, setConfirmed] = useState<CustomerProposal>();
  const initialized = useRef(false);
  const heading = useRef<HTMLHeadingElement>(null);
  const load = useCallback(
    (signal: AbortSignal) =>
      /^[A-Za-z0-9_-]{43}$/.test(token)
        ? publicProposalApi.review(token, signal)
        : Promise.reject(new ApiError('Unavailable', 404)),
    [token],
  );
  const state = useApiResource(`${token}-${revision}`, load);
  const p = confirmed ?? state.data;
  useEffect(() => {
    if (p && !initialized.current) {
      initialized.current = true;
      setLanguage(p.language);
    }
  }, [p, setLanguage]);
  useEffect(() => {
    if (confirmed) {
      heading.current?.focus({ preventScroll: true });
      heading.current?.scrollIntoView({ block: 'start', behavior: 'instant' });
    }
  }, [confirmed]);
  function refresh() {
    setConfirmed(undefined);
    setRevision((v) => v + 1);
  }
  return (
    <>
      <header className={styles.header}>
        <BrandLogo />
        <LanguageSwitcher />
      </header>
      <main id="main-content" tabIndex={-1} className={styles.customer}>
        <h1 ref={heading} tabIndex={-1}>
          {p?.status === 'Approved'
            ? t.approvedTitle
            : p?.status === 'Rejected'
              ? t.rejectedTitle
              : t.customerTitle}
        </h1>
        <p>{t.privateLink}</p>
        {state.error !== undefined && !confirmed ? (
          <ResearchError status={state.error} retry={refresh} />
        ) : !p ? (
          <p role="status">{t.loading}</p>
        ) : (
          <>
            {['Approved', 'Rejected'].includes(p.status) && (
              <p role="status">{t.decisionMessage}</p>
            )}
            {p.status === 'Expired' && (
              <p className={styles.notice}>{t.expired}</p>
            )}
            <ProposalSummary proposal={p} />
            {p.canRespond ? (
              <ProposalDecision
                proposal={p}
                token={token}
                onConfirmed={setConfirmed}
                onRefresh={refresh}
              />
            ) : p.status === 'Sent' ? (
              <p className={styles.notice}>{t.cannotRespond}</p>
            ) : null}
          </>
        )}
        <Link to="/">{t.home}</Link>
      </main>
    </>
  );
}
export function CustomerProposalPage() {
  const { hash } = useLocation();
  return <CustomerView key={hash} token={hash.slice(1)} />;
}
