export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type CommentSortField = 'createdAt' | 'userName' | 'email';

export type SortDirection = 'asc' | 'desc';

// Query parameters of GET /api/comments
export interface CommentsQuery {
  page: number;
  sortBy: CommentSortField;
  sortDirection: SortDirection;
}
