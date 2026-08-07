//EL location es otra cosa en javascript y se ralla
export interface LocationKeue {
    id: string;
    name: string;
    description: string;
    color: string;
}

export type LocationId = string;


export interface LocationInput {
  name: string;
  description: string;
  color: string;
}
export interface UpdateLocationInput extends LocationInput {
  id: LocationId;
}
