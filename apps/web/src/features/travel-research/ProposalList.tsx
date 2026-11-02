import { Link } from 'react-router';
import { Button } from '../../components/ui/Button';
import { useResearchCopy } from './copy';
import type { PageResult } from '../admin/model';
import type { ProposalItem } from './types';
import styles from './Research.module.scss';
export function ProposalList({
  data,
  onPage,
  headingLevel = 3,
}: {
  data: PageResult<ProposalItem>;
  onPage: (page: number) => void;
  headingLevel?: 2 | 3;
}) {
  const t = useResearchCopy();
  const Heading = headingLevel === 2 ? 'h2' : 'h3';
  return (
    <>
      <div className={styles.grid}>
        {data.items.map((p) => (
          <article key={p.id} className={styles.card}>
            <Heading>
              {p.number} · v{p.version}
            </Heading>
            <p>{p.title}</p>
            <p>{t.statuses[p.status]}</p>
            {p.selectedAlternativeId && (
              <p>
                {t.selection}: {p.selectedAlternativeTitle ?? '—'}
              </p>
            )}
            <Link to={'/admin/proposals/' + p.id}>{t.open}</Link>
          </article>
        ))}
      </div>
      {!data.items.length && <p>{t.empty}</p>}
      {data.totalPages > 1 && (
        <nav className={styles.actions} aria-label={t.page}>
          <Button
            variant="outline"
            disabled={data.page <= 1}
            onClick={() => onPage(data.page - 1)}
          >
            {t.previous}
          </Button>
          <span>
            {t.page} {data.page} / {data.totalPages}
          </span>
          <Button
            variant="outline"
            disabled={data.page >= data.totalPages}
            onClick={() => onPage(data.page + 1)}
          >
            {t.next}
          </Button>
        </nav>
      )}
    </>
  );
}
