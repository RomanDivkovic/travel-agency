export function buildContactDraft(
  recipient: string,
  fields: { name: string; email: string; subject: string; message: string },
) {
  const body = `${fields.name.trim()} <${fields.email.trim()}>\n\n${fields.message.trim()}`;
  return `mailto:${encodeURIComponent(recipient)}?subject=${encodeURIComponent(fields.subject.trim())}&body=${encodeURIComponent(body)}`;
}
