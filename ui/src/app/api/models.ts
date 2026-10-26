export interface PersonAstronaut {
  personId: number;
  name: string;
  currentRank: string | null;
  currentDutyTitle: string | null;
  careerStartDate: string | null;
  careerEndDate: string | null;
}

export interface AstronautDuty {
  id: number;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export interface PersonWithDuties {
  person: PersonAstronaut;
  astronautDuties: AstronautDuty[];
}

export interface NewDuty {
  name: string;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
}

export type AstronautStatus = 'active' | 'retired' | 'none';

export function astronautStatus(person: PersonAstronaut): AstronautStatus {
  if (person.careerEndDate) return 'retired';
  if (person.careerStartDate) return 'active';
  return 'none';
}

export function isRetiredTitle(dutyTitle: string): boolean {
  return dutyTitle.trim().toUpperCase() === 'RETIRED';
}

export const statusLabels: Record<AstronautStatus, string> = {
  active: 'Active',
  retired: 'Retired',
  none: 'Not an astronaut',
};
