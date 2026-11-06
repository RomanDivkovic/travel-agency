import { useContent } from '../business-management/contentContext';
import type { BusinessData } from '../business-management/types';
import { BusinessContact } from '../business-management/PublishedComponents';
import { useState, type FormEvent } from 'react';
import { contactEmail as fallbackEmail } from '../../app/config/contact';
import { Button } from '../../components/ui/Button';
import { Section } from '../../components/ui/Section';
import { PageIntro } from './PageIntro';
import { usePublicCopy } from './usePublicCopy';
import { buildContactDraft } from './contactDraft';
import styles from './PublicPages.module.scss';
export function ContactPage() {
  const p = usePublicCopy().contact;
  const business = useContent<BusinessData>('business');
  const contactEmail = business
    ? business.publicContactVisible
      ? business.contactEmail
      : ''
    : fallbackEmail;
  const [status, setStatus] = useState<'idle' | 'draft' | 'invalid'>('idle');
  const [draft, setDraft] = useState<string>();
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!contactEmail) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    const fields = {
      name: String(data.get('name') ?? ''),
      email: String(data.get('email') ?? ''),
      subject: String(data.get('subject') ?? ''),
      message: String(data.get('message') ?? ''),
    };
    if (
      !form.checkValidity() ||
      Object.values(fields).some((value) => !value.trim())
    ) {
      setStatus('invalid');
      setDraft(undefined);
      return;
    }
    setDraft(buildContactDraft(contactEmail, fields));
    setStatus('draft');
  }
  return (
    <>
      <PageIntro {...p} />
      <Section>
        <div className={styles.contactGrid}>
          <aside>
            <h2>{p.infoTitle}</h2>
            {contactEmail ? (
              <p>
                {p.emailLabel}
                <br />
                <a href={`mailto:${contactEmail}`}>{contactEmail}</a>
              </p>
            ) : (
              <p className={styles.notice}>{p.unavailable}</p>
            )}
            <BusinessContact />
            <p>{p.availability}</p>
            <p>{p.wizard}</p>
          </aside>
          <form
            className={styles.form}
            onSubmit={submit}
            onChange={() => {
              setStatus('idle');
              setDraft(undefined);
            }}
          >
            <h2>{p.formTitle}</h2>
            <p id="contact-behavior">{p.disclosure}</p>
            <fieldset
              disabled={!contactEmail}
              aria-describedby="contact-behavior contact-privacy"
            >
              <label htmlFor="contact-name">{p.name}</label>
              <input
                id="contact-name"
                name="name"
                autoComplete="name"
                required
                maxLength={100}
              />
              <label htmlFor="contact-email">{p.email}</label>
              <input
                id="contact-email"
                name="email"
                type="email"
                autoComplete="email"
                required
                maxLength={254}
              />
              <label htmlFor="contact-subject">{p.subject}</label>
              <input
                id="contact-subject"
                name="subject"
                required
                maxLength={120}
              />
              <label htmlFor="contact-message">{p.message}</label>
              <textarea
                id="contact-message"
                name="message"
                required
                maxLength={1500}
                rows={6}
              />
              <p id="contact-privacy" className={styles.note}>
                {p.privacy}
              </p>
              <Button type="submit">{p.prepare}</Button>
            </fieldset>
            {status === 'invalid' ? <p role="alert">{p.error}</p> : null}
            {status === 'draft' && draft ? (
              <div role="status">
                <p>{p.status}</p>
                <a href={draft}>{p.submit}</a>
              </div>
            ) : null}
          </form>
        </div>
      </Section>
    </>
  );
}
