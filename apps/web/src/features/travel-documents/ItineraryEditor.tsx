import { useId, useState, type FormEvent } from 'react';
import { Button } from '../../components/ui/Button';
import { ApiError } from '../../services/apiClient';
import { Field } from '../customer-portal/PortalShared';
import { documentApi } from './api';
import { DocumentError } from './DocumentShared';
import { useDocumentCopy } from './copy';
import { localInput } from './time';
import type { CustomEntry, Itinerary, ItineraryVersion } from './types';
import styles from './Documents.module.scss';
export function ItineraryEditor({
  data,
  version,
  onSaved,
}: {
  data: Itinerary;
  version: ItineraryVersion;
  onSaved: () => void;
}) {
  const t = useDocumentCopy();
  const [title, setTitle] = useState(version.title),
    [description, setDescription] = useState(version.description);
  const [entries, setEntries] = useState<CustomEntry[]>(() =>
    version.entries
      .filter((e) => !e.confirmed)
      .map((e) => {
        const input = e.at && e.timeZone ? localInput(e.at, e.timeZone) : null;
        return {
          id: e.id,
          title: e.title,
          description: e.description,
          date: e.date,
          localTime: input?.time ?? null,
          timeZone: e.timeZone,
          utcOffset: input?.offset ?? null,
          location: e.location,
          sortOrder: e.sortOrder,
        };
      }),
  );
  const [busy, setBusy] = useState(false),
    [error, setError] = useState<number>();
  const descId = useId();
  const update = (index: number, value: Partial<CustomEntry>) =>
    setEntries((rows) =>
      rows.map((row, i) => (i === index ? { ...row, ...value } : row)),
    );
  async function submit(e: FormEvent) {
    e.preventDefault();
    if (busy) return;
    setBusy(true);
    setError(undefined);
    try {
      await documentApi.save(data.bookingId, {
        versionToken: data.versionToken!,
        title,
        description,
        customEntries: entries,
      });
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? (e.status ?? 0) : 0);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className={styles.form} onSubmit={(e) => void submit(e)}>
      <fieldset disabled={busy}>
        <legend>{t.manageItinerary}</legend>
        <Field
          label={t.title}
          required
          maxLength={150}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <label htmlFor={descId}>
          {t.description}
          <textarea
            id={descId}
            rows={4}
            maxLength={3000}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </label>
        <h2>{t.custom}</h2>
        <p>{t.information}</p>
        <p>{t.zoneHint}</p>
        {entries.map((entry, i) => (
          <section key={entry.id ?? i} className={styles.panel}>
            <h3>
              {t.custom} {i + 1}
            </h3>
            <Field
              label={t.entryTitle}
              required
              maxLength={150}
              value={entry.title}
              onChange={(e) => update(i, { title: e.target.value })}
            />
            <Field
              label={t.description}
              maxLength={3000}
              value={entry.description}
              onChange={(e) => update(i, { description: e.target.value })}
            />
            <div className={styles.grid}>
              <Field
                label={t.date}
                type="date"
                required={!entry.localTime}
                disabled={!!entry.localTime}
                value={entry.date ?? ''}
                onChange={(e) => update(i, { date: e.target.value || null })}
              />
              <Field
                label={t.time}
                type="datetime-local"
                value={entry.localTime ?? ''}
                onChange={(e) =>
                  update(i, {
                    localTime: e.target.value || null,
                    date: e.target.value
                      ? e.target.value.slice(0, 10)
                      : entry.date,
                  })
                }
              />
              <Field
                label={t.zone}
                required={!!entry.localTime}
                maxLength={100}
                value={entry.timeZone ?? ''}
                onChange={(e) =>
                  update(i, { timeZone: e.target.value || null })
                }
              />
              <Field
                label={t.offset}
                maxLength={6}
                value={entry.utcOffset ?? ''}
                onChange={(e) =>
                  update(i, { utcOffset: e.target.value || null })
                }
              />
            </div>
            <Field
              label={t.location}
              maxLength={300}
              value={entry.location}
              onChange={(e) => update(i, { location: e.target.value })}
            />
            <Field
              label={t.order}
              type="number"
              required
              min={0}
              max={10000}
              value={entry.sortOrder}
              onChange={(e) => update(i, { sortOrder: Number(e.target.value) })}
            />
            <Button
              variant="text"
              onClick={() =>
                setEntries((rows) => rows.filter((_, n) => n !== i))
              }
            >
              {t.deleteEntry} {i + 1}
            </Button>
          </section>
        ))}
        <Button
          variant="outline"
          onClick={() =>
            setEntries((rows) => [
              ...rows,
              {
                id: crypto.randomUUID(),
                title: '',
                description: '',
                date: null,
                localTime: null,
                timeZone: null,
                utcOffset: null,
                location: '',
                sortOrder: rows.length,
              },
            ])
          }
        >
          {t.add}
        </Button>
        <Button type="submit" disabled={busy}>
          {busy ? t.saving : t.save}
        </Button>
      </fieldset>
      {error !== undefined && <DocumentError status={error} />}
    </form>
  );
}
