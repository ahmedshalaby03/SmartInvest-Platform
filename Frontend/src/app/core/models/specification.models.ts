export interface ProjectSpecification {
  id: number;
  subProjectId: number;
  specificationName: string;
  specificationValue: string;
  unit: string;
}

export interface UpsertSpecification {
  specificationName: string;
  specificationValue: string;
  unit: string;
}
