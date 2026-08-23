import { Injectable } from '@angular/core';

interface CacheEntry {
  value: unknown;
  refreshedAt: number;
}

/**
 * Keeps expensive read results for the lifetime of the signed-in SPA session.
 *
 * Cached data is returned immediately. Once it is old enough, a single refresh runs in the
 * background and the current screen can opt into the fresh value through `onRefresh`.
 */
@Injectable({ providedIn: 'root' })
export class SessionQueryCache {
  private static readonly refreshAfterMs = 15_000;

  private readonly entries = new Map<string, CacheEntry>();
  private readonly pending = new Map<string, Promise<unknown>>();
  private generation = 0;

  get<T>(key: string, load: () => Promise<T>, onRefresh?: (value: T) => void): Promise<T> {
    const cached = this.entries.get(key);
    if (!cached) {
      return this.refresh(key, load);
    }

    if (Date.now() - cached.refreshedAt >= SessionQueryCache.refreshAfterMs) {
      void this.refresh(key, load)
        .then((value) => onRefresh?.(value))
        .catch(() => {
          // The cached value is still usable. A later visit will retry the refresh.
        });
    }

    return Promise.resolve(cached.value as T);
  }

  invalidate(prefix: string): void {
    for (const key of this.entries.keys()) {
      if (key.startsWith(prefix)) {
        this.entries.delete(key);
      }
    }
  }

  clear(): void {
    this.generation++;
    this.entries.clear();
    this.pending.clear();
  }

  private refresh<T>(key: string, load: () => Promise<T>): Promise<T> {
    const existing = this.pending.get(key) as Promise<T> | undefined;
    if (existing) {
      return existing;
    }

    const requestGeneration = this.generation;
    const request = load()
      .then((value) => {
        if (requestGeneration === this.generation) {
          this.entries.set(key, { value, refreshedAt: Date.now() });
        }
        return value;
      })
      .finally(() => {
        if (this.pending.get(key) === request) {
          this.pending.delete(key);
        }
      });

    this.pending.set(key, request);
    return request;
  }
}
