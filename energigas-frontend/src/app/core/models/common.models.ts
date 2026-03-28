export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageSize: number;
  currentPage: number;
  totalPages: number;
}

export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
  search?: string;
}
