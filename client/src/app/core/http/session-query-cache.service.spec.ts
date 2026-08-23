import { SessionQueryCache } from './session-query-cache.service';

describe('SessionQueryCache', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('returns cached data immediately and refreshes stale data in the background', async () => {
    const cache = new SessionQueryCache();
    const clock = jest.spyOn(Date, 'now').mockReturnValue(1_000);

    await expect(cache.get('dashboard', async () => 'first')).resolves.toBe('first');

    clock.mockReturnValue(20_000);
    let completeRefresh!: (value: string) => void;
    const refresh = new Promise<string>((resolve) => {
      completeRefresh = resolve;
    });
    const onRefresh = jest.fn();

    await expect(cache.get('dashboard', () => refresh, onRefresh)).resolves.toBe('first');
    expect(onRefresh).not.toHaveBeenCalled();

    completeRefresh('fresh');
    await refresh;
    await Promise.resolve();
    await Promise.resolve();

    expect(onRefresh).toHaveBeenCalledWith('fresh');
  });

  it('does not restore data from an old request after the session cache is cleared', async () => {
    const cache = new SessionQueryCache();
    let completeOldRequest!: (value: string) => void;
    const oldValue = new Promise<string>((resolve) => {
      completeOldRequest = resolve;
    });

    const oldRequest = cache.get('dashboard', () => oldValue);
    cache.clear();
    await cache.get('dashboard', async () => 'new-user-data');

    completeOldRequest('old-user-data');
    await oldRequest;

    await expect(cache.get('dashboard', async () => 'unexpected')).resolves.toBe('new-user-data');
  });
});
