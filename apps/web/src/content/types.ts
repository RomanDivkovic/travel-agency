import type { sv } from './sv';

export type Translate<T> = T extends string
  ? string
  : {
      [K in keyof T]: K extends 'id' | 'icon' | 'image' | 'destination'
        ? T[K]
        : Translate<T[K]>;
    };
export type Translations = Translate<typeof sv>;
