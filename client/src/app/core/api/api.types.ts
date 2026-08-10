import type { components } from './schema.d.ts';

export type Schemas = components['schemas'];

export type ApiErrorResponse = Schemas['ApiErrorResponse'];
export type AuthenticatedUser = Schemas['AuthenticatedUserResponse'];
export type TokenApiResponse = Schemas['TokenApiResponse'];
export type UserRole = Schemas['UserRole'];

export type PagedResult<T> = {
  items?: T[] | null;
  totalCount?: number;
  pageNumber?: number;
  pageSize?: number;
  totalPages?: number;
};
