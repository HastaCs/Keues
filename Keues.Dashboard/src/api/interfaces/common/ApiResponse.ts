export interface Pagination {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

//Para las listas
export interface ApiResponse<T> {
  data: T;
  pagination?: Pagination;
}
