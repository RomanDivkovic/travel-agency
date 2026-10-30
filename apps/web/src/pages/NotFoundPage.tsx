import { Link } from 'react-router';

export function NotFoundPage() {
  return (
    <>
      <h1>Sidan hittades inte</h1>
      <Link to="/">Till startsidan</Link>
    </>
  );
}
