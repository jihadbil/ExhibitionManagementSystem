export interface ItemPage<T> { items: T[]; totalCount?: number; }

export const collectPages = async <T>(load: (page: number, pageSize: number) => Promise<ItemPage<T>>, pageSize = 200): Promise<T[]> => {
  const result: T[] = [];
  for (let page = 1; ; page += 1) {
    const response = await load(page, pageSize);
    const items = response.items ?? [];
    result.push(...items);
    if (items.length === 0 || (response.totalCount !== undefined && result.length >= response.totalCount) ||
        (response.totalCount === undefined && items.length < pageSize)) return result;
  }
};
