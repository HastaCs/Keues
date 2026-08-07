import type { LocationId } from "@/api/interfaces/Location/Locations";
import type { QueueId } from "@/api/interfaces/Queue/Queues";


export type CounterId = string;

export interface Counter {
  id: CounterId;
  code: string;
  name: string;
  color: string;
  description: string;
  locationId: LocationId;
  queues: QueueId[]|null;
}

export interface CreateCounterInput {
  code: string;
  name: string;
  description: string;
  color: string;
  locationId: LocationId;
  queues: QueueId[];
}

export interface UpdateCounterInput extends CreateCounterInput {
  id: CounterId;
}
