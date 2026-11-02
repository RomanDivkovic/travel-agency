import { emptyLeg } from './formHelpers';
import { Button } from '../../components/ui/Button';
import { useResearchCopy } from './copy';
import { TextField } from './ResearchShared';
import type {
  AccommodationDetails,
  ExtraDetails,
  FlightDetails,
  FlightLeg,
} from './types';
import styles from './Research.module.scss';
export function FlightFields({
  value: f,
  onChange,
}: {
  value: FlightDetails;
  onChange: (v: FlightDetails) => void;
}) {
  const t = useResearchCopy();
  function leg(
    index: number,
    key: Exclude<keyof FlightLeg, 'journey'>,
    v: string,
  ) {
    onChange({
      ...f,
      legs: f.legs.map((l, n) => (n === index ? { ...l, [key]: v } : l)),
    });
  }
  return (
    <>
      <p>{t.zoneHint}</p>
      {f.legs.map((l, index) => (
        <fieldset key={index}>
          <legend>
            {t.leg} {index + 1}
          </legend>
          <div className={styles.form}>
            <TextField
              label={t.journey}
              value={l.journey}
              type="number"
              min={1}
              max={10}
              onChange={(v) =>
                onChange({
                  ...f,
                  legs: f.legs.map((leg, n) =>
                    n === index ? { ...leg, journey: Number(v) } : leg,
                  ),
                })
              }
            />
            {(
              [
                'airline',
                'flightNumber',
                'departureAirport',
                'arrivalAirport',
                'departureAt',
                'arrivalAt',
                'departureTimeZone',
                'arrivalTimeZone',
              ] as const
            ).map((key) => (
              <TextField
                key={key}
                label={
                  t[
                    key === 'departureTimeZone'
                      ? 'departureZone'
                      : key === 'arrivalTimeZone'
                        ? 'arrivalZone'
                        : key
                  ]
                }
                value={l[key]}
                required={key !== 'flightNumber'}
                maxLength={key === 'flightNumber' ? 40 : 100}
                onChange={(v) => leg(index, key, v)}
              />
            ))}
            <Button
              variant="outline"
              disabled={f.legs.length === 1}
              onClick={() =>
                onChange({ ...f, legs: f.legs.filter((_, n) => n !== index) })
              }
            >
              {t.remove} {t.leg} {index + 1}
            </Button>
          </div>
        </fieldset>
      ))}
      <Button
        variant="outline"
        disabled={f.legs.length >= 10}
        onClick={() =>
          onChange({
            ...f,
            legs: [
              ...f.legs,
              { ...emptyLeg(), journey: f.legs.at(-1)?.journey ?? 1 },
            ],
          })
        }
      >
        {t.addLeg}
      </Button>
      <TextField
        label={t.cabin}
        value={f.cabinClass}
        maxLength={100}
        onChange={(v) => onChange({ ...f, cabinClass: v })}
      />
      <TextField
        label={t.baggage}
        value={f.baggageDetails}
        maxLength={1000}
        onChange={(v) => onChange({ ...f, baggageDetails: v })}
      />
    </>
  );
}
export function StayFields({
  value: a,
  onChange,
}: {
  value: AccommodationDetails;
  onChange: (v: AccommodationDetails) => void;
}) {
  const t = useResearchCopy();
  const labels = {
    destination: t.destination,
    address: t.address,
    accommodationType: t.propertyType,
    roomType: t.room,
    mealPlan: t.meal,
    amenities: t.amenities,
  };
  return (
    <>
      {(Object.keys(labels) as (keyof typeof labels)[]).map((key) => (
        <TextField
          key={key}
          label={labels[key]}
          value={a[key]}
          maxLength={
            key === 'amenities'
              ? 1000
              : key === 'address'
                ? 500
                : key === 'mealPlan'
                  ? 300
                  : key === 'roomType'
                    ? 200
                    : key === 'destination'
                      ? 150
                      : 100
          }
          required={key !== 'amenities'}
          onChange={(v) => onChange({ ...a, [key]: v })}
        />
      ))}
      <TextField
        label={t.checkIn}
        type="date"
        value={a.checkIn}
        onChange={(v) => onChange({ ...a, checkIn: v })}
      />
      <TextField
        label={t.checkOut}
        type="date"
        value={a.checkOut}
        onChange={(v) => onChange({ ...a, checkOut: v })}
      />
      <TextField
        label={t.guests}
        type="number"
        min={1}
        max={100}
        value={a.guests}
        onChange={(v) => onChange({ ...a, guests: Number(v) })}
      />
    </>
  );
}
export function ExtraFields({
  value: e,
  onChange,
}: {
  value: ExtraDetails;
  onChange: (v: ExtraDetails) => void;
}) {
  const t = useResearchCopy();
  return (
    <>
      <label>
        {t.category}
        <select
          value={e.category}
          onChange={(event) => onChange({ ...e, category: event.target.value })}
        >
          {Object.entries(t.categories).map(([key, label]) => (
            <option key={key} value={key}>
              {label}
            </option>
          ))}
        </select>
      </label>
      <TextField
        label={t.location}
        value={e.location}
        onChange={(v) => onChange({ ...e, location: v })}
      />
      <TextField
        label={t.date}
        type="date"
        value={e.date ?? ''}
        required={false}
        onChange={(v) => onChange({ ...e, date: v || null })}
      />
    </>
  );
}
