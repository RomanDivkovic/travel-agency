import { emptyWizard, type WizardData, type Service } from './model';
export type Update = {
  [K in Exclude<keyof WizardData, 'service'>]: {
    type: 'update';
    group: K;
    value: Partial<WizardData[K]>;
  };
}[Exclude<keyof WizardData, 'service'>];
export type WizardAction =
  Update | { type: 'service'; value: Service } | { type: 'reset' };
export function wizardReducer(
  data: WizardData,
  action: WizardAction,
): WizardData {
  if (action.type === 'reset') return emptyWizard();
  if (action.type === 'service') {
    if (data.service === action.value) return data;
    const clean = emptyWizard();
    // Shared answers survive a service change; service-specific answers do not leak into another journey.
    return {
      ...data,
      service: action.value,
      flight: clean.flight,
      accommodation: clean.accommodation,
      trip: clean.trip,
      dates: { ...data.dates, oneWay: false },
      travelers: { ...data.travelers, rooms: '1' },
    };
  }
  if (action.group === 'travelers' && action.value.children !== undefined) {
    const count = /^\d+$/.test(action.value.children)
      ? Math.min(10, Number(action.value.children))
      : 0;
    return {
      ...data,
      travelers: {
        ...data.travelers,
        ...action.value,
        ages: Array.from(
          { length: count },
          (_, index) => data.travelers.ages[index] ?? '',
        ),
      },
    };
  }
  return {
    ...data,
    [action.group]: { ...data[action.group], ...action.value },
  };
}
