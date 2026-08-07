import { createContext, useContext } from 'react';
import type { LocationKeue } from '@/api/interfaces/Location/Locations';

export const LocationContext = createContext<LocationKeue | null>(null);

export function useActiveLocation(): LocationKeue | null {
  return useContext(LocationContext);
}
