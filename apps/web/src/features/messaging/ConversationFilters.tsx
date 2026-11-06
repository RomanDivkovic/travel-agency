import { useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { useMessagingCopy } from './copy';
import type { ConversationFilters as Filters } from './types';
import styles from './Messaging.module.scss';
export function ConversationFilters({
  initial,
  admin,
  onApply,
}: {
  initial: Filters;
  admin: boolean;
  onApply: (
    values: Pick<Filters, 'search' | 'status' | 'priority' | 'unread'>,
  ) => void;
}) {
  const t = useMessagingCopy();
  const [search, setSearch] = useState(initial.search),
    [status, setStatus] = useState(initial.status),
    [priority, setPriority] = useState(initial.priority),
    [unread, setUnread] = useState(initial.unread);
  function submit(e: FormEvent) {
    e.preventDefault();
    onApply({ search: search.trim(), status, priority, unread });
  }
  return (
    <form className={`${styles.form} ${styles.panel}`} onSubmit={submit}>
      <div className={styles.filters}>
        <label>
          {t.search}
          <input
            maxLength={150}
            value={search}
            placeholder={admin ? t.searchHint : undefined}
            onChange={(e) => setSearch(e.target.value)}
          />
        </label>
        <label>
          {t.status}
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">{t.all}</option>
            {Object.entries(t.statuses).map(([v, l]) => (
              <option key={v} value={v}>
                {l}
              </option>
            ))}
          </select>
        </label>
        {admin && (
          <label>
            {t.priority}
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value)}
            >
              <option value="">{t.allPriorities}</option>
              {Object.entries(t.priorities).map(([v, l]) => (
                <option key={v} value={v}>
                  {l}
                </option>
              ))}
            </select>
          </label>
        )}
        <label className={styles.check}>
          <input
            type="checkbox"
            checked={unread}
            onChange={(e) => setUnread(e.target.checked)}
          />
          {t.unreadOnly}
        </label>
        <Button type="submit">{t.apply}</Button>
      </div>
    </form>
  );
}
