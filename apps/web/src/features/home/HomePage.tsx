import { sv } from '../../content/sv';
import { usePageMetadata } from '../../hooks/usePageMetadata';
import { Hero } from './components/Hero';
import { Services } from './components/Services';
import { HowItWorks } from './components/HowItWorks';
import { Benefits } from './components/Benefits';
import { AboutPreview } from './components/AboutPreview';
import { TravelInspiration } from './components/TravelInspiration';
import { FinalCTA } from './components/FinalCTA';

export function HomePage() {
  usePageMetadata(sv.meta.homeTitle);
  return (
    <>
      <Hero />
      <Services />
      <HowItWorks />
      <Benefits />
      <AboutPreview />
      <TravelInspiration />
      <FinalCTA />
    </>
  );
}
