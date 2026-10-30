import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

// JSDOM has no layout/scrolling implementation; live browser QA covers these APIs.
window.scrollTo = vi.fn();
Element.prototype.scrollIntoView = vi.fn();

afterEach(cleanup);
