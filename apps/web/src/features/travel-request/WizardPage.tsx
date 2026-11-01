import {
  useEffect,
  useLayoutEffect,
  useReducer,
  useRef,
  useState,
  useSyncExternalStore,
  type FormEvent,
} from 'react';
import { useSearchParams } from 'react-router';
import { Section } from '../../components/ui/Section';
import { Button } from '../../components/ui/Button';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { isService, wizardSteps, stepForField } from './config';
import { useWizardCopy } from './copy';
import { emptyWizard, type Service, type StepId } from './model';
import { wizardReducer } from './reducer';
import {
  loadDraft,
  storeDraft,
  subscribeDraftStorage,
  draftStorageAvailable,
} from './draft';
import { validateStep, validateRequest, type FormErrors } from './validation';
import { Check } from './components/Fields';
import { ServiceStep } from './steps/ServiceStep';
import { TripDetails } from './steps/TripDetails';
import { PreferencesStep } from './steps/PreferencesStep';
import { PersonalDetails } from './steps/PersonalDetails';
import { Review } from './components/Review';
import styles from './Wizard.module.scss';
import {
  hasSubmissionAttempt,
  resetSubmissionAttempt,
} from './submissionStorage';
import { useSubmissionCopy } from './submissionCopy';
export function WizardPage() {
  const [params, setParams] = useSearchParams();
  // Entry links seed a new mount; local service changes must not remount and lose answers.
  const [preselected] = useState(() => {
    const value = params.get('service');
    return isService(value) ? value : undefined;
  });
  function syncService(service: Service | '') {
    const next = new URLSearchParams(params);
    if (service) next.set('service', service);
    else next.delete('service');
    if (next.toString() !== params.toString())
      setParams(next, { replace: true });
  }
  return (
    <TravelWizard preselected={preselected} onServiceChange={syncService} />
  );
}
function TravelWizard({
  preselected,
  onServiceChange,
}: {
  preselected: Service | undefined;
  onServiceChange: (service: Service | '') => void;
}) {
  const t = useWizardCopy();
  const submitCopy = useSubmissionCopy();
  const [busy, setBusy] = useState(false);
  const [initial] = useState(() => {
    const draft = loadDraft();
    let data = draft.data ?? emptyWizard();
    if (preselected)
      data = wizardReducer(data, { type: 'service', value: preselected });
    return { ...draft, data, restored: Boolean(draft.data) };
  });
  const [data, dispatch] = useReducer(wizardReducer, initial.data);
  const [step, setStep] = useState<StepId>('service');
  const [errors, setErrors] = useState<FormErrors>({});
  const [saving, setSaving] = useState(initial.restored);
  const writeAvailable = useSyncExternalStore(
    subscribeDraftStorage,
    draftStorageAvailable,
    () => true,
  );
  const storageError = !initial.available || !writeAvailable;
  const [restored, setRestored] = useState(initial.restored);
  const [resetPrompt, setResetPrompt] = useState(false);
  const [resetDone, setResetDone] = useState(false);
  const [editing, setEditing] = useState(false);
  const heading = useRef<HTMLHeadingElement>(null);
  const alert = useRef<HTMLDivElement>(null);
  const previousStep = useRef(step);
  const resetButton = useRef<HTMLButtonElement>(null);
  const steps = wizardSteps(data.service);
  const index = steps.indexOf(step);
  usePageMetadata(`${t.steps[step]} · ${t.title}`, t.description);
  useEffect(() => {
    storeDraft(data, saving);
  }, [data, saving]);
  useLayoutEffect(() => {
    if (Object.keys(errors).length) alert.current?.focus();
    else if (previousStep.current !== step) heading.current?.focus();
    previousStep.current = step;
  }, [step, errors]);
  function go(next: StepId) {
    setErrors({});
    setStep(next);
    setResetDone(false);
  }
  function showErrors(next: FormErrors) {
    setErrors(next);
  }
  function forward(event: FormEvent) {
    event.preventDefault();
    const invalid = validateStep(step, data);
    if (Object.keys(invalid).length) {
      showErrors(invalid);
      return;
    }
    if (editing) {
      const invalidRequest = validateRequest(data);
      if (invalidRequest.length) {
        const firstError = invalidRequest[0];
        if (firstError) {
          setStep(firstError.step);
          showErrors(firstError.errors);
        }
        return;
      }
      setEditing(false);
      go('review');
      return;
    }
    const next = steps[index + 1];
    if (next === 'review') {
      const invalidRequest = validateRequest(data);
      const firstError = invalidRequest[0];
      if (firstError) {
        setStep(firstError.step);
        showErrors(firstError.errors);
        return;
      }
    }
    if (next) go(next);
  }
  function reset() {
    resetSubmissionAttempt();
    dispatch({ type: 'reset' });
    onServiceChange('');
    setSaving(false);
    setRestored(false);
    setResetPrompt(false);
    setResetDone(true);
    setEditing(false);
    setErrors({});
    setStep('service');
    heading.current?.focus();
  }
  function errorLabel(key: string) {
    if (key.startsWith('age-'))
      return `${t.labels.age} ${Number(key.slice(4)) + 1}`;
    return t.labels[key as keyof typeof t.labels] ?? t.steps[step];
  }
  return (
    <Section className={styles.page}>
      <div className={styles.intro}>
        <p className={styles.eyebrow}>
          {t.step} {index + 1} {t.of} {steps.length}
        </p>
        <h1>{t.title}</h1>
        <p>{t.description}</p>
        <p className={styles.notice}>{t.unavailable}</p>
      </div>
      <div className={styles.layout}>
        <aside className={styles.sidebar}>
          <nav aria-label={t.progress}>
            <ol>
              {steps.map((id, i) => (
                <li key={id} aria-current={step === id ? 'step' : undefined}>
                  <span aria-hidden="true">
                    {String(i + 1).padStart(2, '0')}
                  </span>
                  {i < index ? (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => {
                        setEditing(false);
                        go(id);
                      }}
                    >
                      {t.steps[id]}
                    </button>
                  ) : (
                    <span>{t.steps[id]}</span>
                  )}
                </li>
              ))}
            </ol>
          </nav>
          <progress
            value={index + 1}
            max={steps.length}
            aria-label={t.progress}
          />
        </aside>
        <div className={styles.panel}>
          <div className={styles.panelHeader}>
            <h2 ref={heading} tabIndex={-1}>
              {t.steps[step]}
            </h2>
            <Button
              disabled={busy}
              ref={resetButton}
              variant="text"
              onClick={() => setResetPrompt(true)}
            >
              {t.startOver}
            </Button>
          </div>
          {resetPrompt ? (
            <div
              role="group"
              aria-label={t.startOver}
              className={styles.notice}
            >
              <p>
                {hasSubmissionAttempt()
                  ? submitCopy.pendingReset
                  : t.resetQuestion}
              </p>
              <div className={styles.actions}>
                <Button onClick={reset}>{t.resetConfirm}</Button>
                <Button
                  variant="outline"
                  onClick={() => {
                    setResetPrompt(false);
                    resetButton.current?.focus();
                  }}
                >
                  {t.cancel}
                </Button>
              </div>
            </div>
          ) : null}
          {resetDone ? <p role="status">{t.resetDone}</p> : null}
          {restored ? <p className={styles.notice}>{t.restored}</p> : null}
          {step !== 'review' ? (
            <p className={styles.helper}>{t.required}</p>
          ) : null}
          {Object.keys(errors).length ? (
            <div
              className={styles.errorSummary}
              role="alert"
              tabIndex={-1}
              ref={alert}
            >
              <h3>{t.errorSummary}</h3>
              <ul>
                {Object.entries(errors).map(([key, error]) => (
                  <li key={key}>
                    <a
                      href={`#${key === 'service' ? 'service-flights' : key}`}
                      onClick={(e) => {
                        e.preventDefault();
                        document
                          .getElementById(
                            key === 'service' ? 'service-flights' : key,
                          )
                          ?.focus();
                      }}
                    >
                      {errorLabel(key)}: {t.errors[error]}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
          {step === 'review' ? (
            <Review
              data={data}
              busy={busy}
              onBusy={setBusy}
              onValidation={(serverErrors) => {
                const field = Object.keys(serverErrors).find((key) =>
                  stepForField(key),
                );
                const target = field ? stepForField(field) : undefined;
                if (target) {
                  setEditing(true);
                  setStep(target);
                  showErrors(
                    Object.fromEntries(
                      Object.entries(serverErrors).filter(
                        ([key]) => stepForField(key) === target,
                      ),
                    ),
                  );
                }
              }}
              onEdit={(id) => {
                setEditing(true);
                go(id);
              }}
            />
          ) : (
            <form noValidate onSubmit={forward}>
              {step === 'service' ? (
                <ServiceStep
                  data={data}
                  errors={errors}
                  onSelect={(value) => {
                    dispatch({ type: 'service', value });
                    setErrors({});
                    onServiceChange(value);
                  }}
                />
              ) : null}
              <TripDetails
                step={step}
                data={data}
                dispatch={dispatch}
                errors={errors}
              />
              <PreferencesStep
                step={step}
                data={data}
                dispatch={dispatch}
                errors={errors}
              />
              <PersonalDetails
                step={step}
                data={data}
                dispatch={dispatch}
                errors={errors}
              />
              <div className={styles.actions}>
                {index > 0 ? (
                  <Button
                    variant="outline"
                    onClick={() => {
                      setEditing(false);
                      const previous = steps[index - 1];
                      if (previous) go(previous);
                    }}
                  >
                    {t.back}
                  </Button>
                ) : null}
                <Button type="submit">
                  {editing ? t.reviewReturn : t.next}
                </Button>
              </div>
            </form>
          )}
          {step === 'review' ? (
            <div className={styles.actions}>
              <Button
                disabled={busy}
                variant="outline"
                onClick={() => go('contact')}
              >
                {t.back}
              </Button>
            </div>
          ) : null}
          <div className={styles.draft}>
            <Check
              id="save-draft"
              label={t.save}
              checked={saving}
              onChange={setSaving}
            />
            <p>{t.draftNotice}</p>
            {storageError ? <p role="status">{t.storageError}</p> : null}
          </div>
        </div>
      </div>
    </Section>
  );
}
