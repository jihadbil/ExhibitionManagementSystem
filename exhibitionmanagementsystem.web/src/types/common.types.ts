export interface ApiResult<T> {
  data?: T;
  isSuccess: boolean;
  message?: string;
  errors?: string[];
  statusCode?: number;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export type StatusType = 'Active' | 'Available' | 'Reserved' | 'PendingReview' | 'Upcoming' | 'Completed' | 'Cancelled' | 'Paid' | 'Pending' | 'Overdue';

export type ExhibitionCategory = 'Technical' | 'Medical' | 'Industrial' | 'Commercial' | 'Educational' | 'Automotive' | string;
