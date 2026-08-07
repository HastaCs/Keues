import type { LocationId } from "@/api/interfaces/Location/Locations";

export type MenuNodeType = "menu" | "ticket";
export type FlowIconKey = "fruit" | "fish" | "meat" | "car" | "house" | "ticket";

export interface FlowMenuItem {
  id: string;
  name: string;
  description: string;
  nodeType: MenuNodeType;
  parentId: string | null;
  queueSystemId: string;
  queueId: string | null;
  icon: FlowIconKey;
  color: string;
  removedAt: string | null;
}

export interface FlowInput {
  name: string;
  description: string;
  flowType: number;
  locationId: LocationId;
  flowJson:string

}

export interface UpdateFlowInput extends FlowInput {
  id: string;
}

export interface Flow {
  id: string;
  name: string;
  description: string;
  flowType: number;
  locationId: LocationId;
  menuItems: FlowMenuItem[];
  createdAt: string;
  flowJson: string;

}

//TODO esto que es? se puede borrar?
export interface FlowsStorageState {
  version: 1;
  byLocation: Record<string, FlowInput[]>;
}
