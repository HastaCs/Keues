import { CounterId } from "../Counter/Counters";

export interface Queue {
  id: QueueId;
  name: string;
  description: string;
  maxValue: number | null;
  code: string;
  priority: number;
  weight: number;
  agingIntervalMinutes: number;
  maxAgingBonus: number;
  color: string;
  locationId: string;
  counters: CounterId[];
}

export type QueueId = string;

export interface QueueInput {
   name: string;
  description: string;
  maxValue: number | null;
  code: string;
  priority: number;
  weight: number;
  agingIntervalMinutes: number;
  maxAgingBonus: number;
  color: string;
  locationId: string;
  counters: CounterId[];
}
export interface UpdateQueueInput extends QueueInput {
  id: QueueId;
}
