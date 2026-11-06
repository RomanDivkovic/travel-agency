import { useLanguage } from '../../localization/useLanguage';
import { useCmsCopy, fieldLabel } from './copy';
import { TextField, Check, Select } from './Fields';
import type { BusinessData, PreferencesData } from './types';
import styles from './Management.module.scss';
export function BusinessEditor({
  data,
  onChange,
}: {
  data: BusinessData;
  onChange: (d: BusinessData) => void;
}) {
  const t = useCmsCopy();
  const { language } = useLanguage();
  const sv = language === 'sv';
  const change = (k: string, v: string) => onChange({ ...data, [k]: v });
  return (
    <>
      <div className={styles.grid}>
        {(
          [
            'businessName',
            'legalBusinessName',
            'organizationNumber',
            'contactEmail',
            'contactPhone',
            'websiteUrl',
            'address',
            'postalCode',
            'city',
            'country',
            'defaultLanguage',
            'defaultCurrency',
            'timeZone',
          ] as const
        ).map((k) =>
          k === 'defaultLanguage' ? (
            <Select
              key={k}
              label={fieldLabel(k, sv)}
              value={data[k]}
              onChange={(v) => change(k, v)}
            >
              <option value="sv">Svenska</option>
              <option value="en">English</option>
            </Select>
          ) : k === 'defaultCurrency' ? (
            <Select
              key={k}
              label={fieldLabel(k, sv)}
              value={data[k]}
              onChange={(v) => change(k, v)}
            >
              {['SEK', 'EUR', 'USD', 'GBP', 'NOK', 'DKK'].map((v) => (
                <option key={v}>{v}</option>
              ))}
            </Select>
          ) : (
            <TextField
              key={k}
              label={fieldLabel(k, sv)}
              value={data[k]}
              required={
                k === 'businessName' || k === 'timeZone' || k === 'country'
              }
              maxLength={k === 'contactEmail' ? 254 : 500}
              type={
                k === 'contactEmail'
                  ? 'email'
                  : k === 'websiteUrl'
                    ? 'url'
                    : 'text'
              }
              onChange={(v) => change(k, v)}
            />
          ),
        )}
      </div>
      <Check
        label={t.visible}
        checked={data.publicContactVisible}
        onChange={(v) => onChange({ ...data, publicContactVisible: v })}
      />
      <h2>{sv ? 'Öppettider' : 'Opening hours'}</h2>
      <p>
        {sv
          ? 'Dagar utan intervall visas som stängda. Tider anges i verksamhetens tidszon.'
          : 'Days without a range are closed. Times use the business timezone.'}
      </p>
      {data.openingHours.map((h, i) => (
        <div key={i} className={styles.row}>
          <Select
            label={t.day}
            value={String(h.day)}
            onChange={(v) =>
              onChange({
                ...data,
                openingHours: data.openingHours.map((x, n) =>
                  n === i ? { ...x, day: Number(v) } : x,
                ),
              })
            }
          >
            {[1, 2, 3, 4, 5, 6, 0].map((d) => (
              <option key={d} value={d}>
                {new Intl.DateTimeFormat(language, {
                  weekday: 'long',
                  timeZone: 'UTC',
                }).format(new Date(Date.UTC(2026, 10, 1 + d)))}
              </option>
            ))}
          </Select>
          {(['opens', 'closes'] as const).map((k) => (
            <TextField
              key={k}
              label={t[k]}
              type="time"
              value={h[k]}
              required
              onChange={(v) =>
                onChange({
                  ...data,
                  openingHours: data.openingHours.map((x, n) =>
                    n === i ? { ...x, [k]: v } : x,
                  ),
                })
              }
            />
          ))}
          <button
            type="button"
            onClick={() =>
              onChange({
                ...data,
                openingHours: data.openingHours.filter((_, n) => n !== i),
              })
            }
          >
            {t.remove}
          </button>
        </div>
      ))}
      <button
        type="button"
        onClick={() =>
          onChange({
            ...data,
            openingHours: [
              ...data.openingHours,
              { day: 1, opens: '09:00', closes: '17:00' },
            ],
          })
        }
      >
        {t.addHours}
      </button>
      <h2>{sv ? 'Avvikande öppettider' : 'Special opening hours'}</h2>
      {data.exceptions.map((h, i) => (
        <div key={i} className={styles.row}>
          <TextField
            label={t.date}
            type="date"
            required
            value={h.date}
            onChange={(v) =>
              onChange({
                ...data,
                exceptions: data.exceptions.map((x, n) =>
                  n === i ? { ...x, date: v } : x,
                ),
              })
            }
          />
          <Check
            label={t.closed}
            checked={h.closed}
            onChange={(v) =>
              onChange({
                ...data,
                exceptions: data.exceptions.map((x, n) =>
                  n === i ? { ...x, closed: v } : x,
                ),
              })
            }
          />
          {!h.closed &&
            (['opens', 'closes'] as const).map((k) => (
              <TextField
                key={k}
                label={t[k]}
                type="time"
                value={h[k]}
                required
                onChange={(v) =>
                  onChange({
                    ...data,
                    exceptions: data.exceptions.map((x, n) =>
                      n === i ? { ...x, [k]: v } : x,
                    ),
                  })
                }
              />
            ))}
          <button
            type="button"
            onClick={() =>
              onChange({
                ...data,
                exceptions: data.exceptions.filter((_, n) => n !== i),
              })
            }
          >
            {t.remove}
          </button>
        </div>
      ))}
      <button
        type="button"
        onClick={() =>
          onChange({
            ...data,
            exceptions: [
              ...data.exceptions,
              { date: '', closed: true, opens: '09:00', closes: '17:00' },
            ],
          })
        }
      >
        {t.addException}
      </button>
    </>
  );
}
export function PreferencesEditor({
  data,
  onChange,
}: {
  data: PreferencesData;
  onChange: (d: PreferencesData) => void;
}) {
  const t = useCmsCopy();
  return (
    <>
      <p>{t.notificationNote}</p>
      <Check
        label={t.agentMail}
        checked={data.agentMessageEmail}
        onChange={(v) => onChange({ ...data, agentMessageEmail: v })}
      />
      <Check
        label={t.customerMail}
        checked={data.customerMessageEmail}
        onChange={(v) => onChange({ ...data, customerMessageEmail: v })}
      />
    </>
  );
}
