import { api } from './api';
import { collectPages, type ItemPage } from './paginationUtils';

export const getAllItems = <T>(url: string): Promise<T[]> => collectPages(async (page, pageSize) => {
  const response = await api.get<ItemPage<T>>(url, { params: { page, pageSize } });
  return response.data;
});
