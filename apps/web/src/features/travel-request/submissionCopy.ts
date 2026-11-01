import { useLanguage } from '../../localization/useLanguage';
const sv = {
  submit: 'Skicka kostnadsfri förfrågan',
  submitting: 'Skickar…',
  retry: 'Försök igen med samma förfrågan',
  intro:
    'När du skickar sparas uppgifterna så att reseplaneraren kan behandla din förfrågan. Det innebär ingen bokning eller betalning.',
  validation: 'Kontrollera de markerade uppgifterna och försök igen.',
  network:
    'Vi kunde inte bekräfta resultatet. Förfrågan kan ha sparats. Dina svar finns kvar; försök igen med samma förfrågan för att undvika en dubblett.',
  server:
    'Resultatet kunde inte bekräftas just nu. Dina svar finns kvar. Försök igen med samma förfrågan.',
  conflict:
    'Den här inskickningen har redan använts för andra svar. Ändra tillbaka till de ursprungliga svaren och försök igen. Börja inte om om du bara vill kontrollera en tidigare inskickning.',
  storage:
    'Webbläsaren kan inte bevara en säker återförsöksnyckel, eller en tidigare inskickning behöver klarläggas. Inget nytt har skickats. Tillåt lokal lagring eller börja om först när du är säker på att du vill skapa en ny förfrågan.',
  rate: 'För många försök. Vänta innan du försöker igen:',
  seconds: 'sekunder.',
  pendingReset:
    'En tidigare inskickning kan redan vara sparad. Om du börjar om glöms återförsöksnyckeln och en ny inskickning kan skapa en dubblett. Vill du ändå rensa allt?',
  successTitle: 'Tack! Vi har tagit emot din förfrågan.',
  successMessage:
    'Din reseförfrågan har sparats. Vi kommer att gå igenom dina önskemål och återkomma med information om nästa steg.',
  reference: 'Referensnummer',
  note: 'Din förfrågan innebär inte att någon resa har bokats.',
  emailNote:
    'E-postbekräftelsen behandlas separat. Den här sidan bekräftar att förfrågan är sparad, inte att ett e-postmeddelande har levererats.',
  missingTitle: 'Ingen bekräftad förfrågan att visa',
  missing:
    'Det finns ingen giltig inskickningsbekräftelse i den här fliken. Om ett tidigare försök inte kunde bekräftas, gå tillbaka och försök med samma förfrågan.',
  home: 'Till startsidan',
  wizard: 'Till reseförfrågan',
  cleanup:
    'Förfrågan är sparad, men webbläsaren kunde inte rensa det lokala utkastet. Rensa webbplatsdata innan du börjar en ny förfrågan.',
};
const en: { [K in keyof typeof sv]: string } = {
  submit: 'Send free enquiry',
  submitting: 'Sending…',
  retry: 'Retry the same enquiry',
  intro:
    'Sending saves your details so the travel planner can process your enquiry. It does not make a booking or payment.',
  validation: 'Check the highlighted answers and try again.',
  network:
    'We could not confirm the result. Your enquiry may have been saved. Your answers are retained; retry the same enquiry to avoid a duplicate.',
  server:
    'The result could not be confirmed right now. Your answers are retained. Retry the same enquiry.',
  conflict:
    'This submission attempt was already used for different answers. Restore the original answers and retry. Do not start over just to check an earlier submission.',
  storage:
    'The browser cannot keep a safe retry key, or an earlier attempt needs to be resolved. Nothing new has been sent. Allow local storage, or start over only when you intend to create a new enquiry.',
  rate: 'Too many attempts. Wait before retrying:',
  seconds: 'seconds.',
  pendingReset:
    'An earlier attempt may already be saved. Starting over forgets the retry key, and another submission may create a duplicate. Do you still want to clear everything?',
  successTitle: 'Thank you! We have received your enquiry.',
  successMessage:
    'Your travel enquiry has been saved. We will review your preferences and get back to you about the next steps.',
  reference: 'Reference number',
  note: 'Your enquiry does not mean that any travel has been booked.',
  emailNote:
    'Email confirmation is processed separately. This page confirms your enquiry was saved, not that an email has been delivered.',
  missingTitle: 'No confirmed enquiry to display',
  missing:
    'There is no valid submission confirmation in this tab. If an earlier attempt could not be confirmed, return and retry the same enquiry.',
  home: 'Back to home',
  wizard: 'Back to travel enquiry',
  cleanup:
    'Your enquiry is saved, but the browser could not clear its local draft. Clear site data before starting a new enquiry.',
};
export function useSubmissionCopy() {
  return useLanguage().language === 'sv' ? sv : en;
}
