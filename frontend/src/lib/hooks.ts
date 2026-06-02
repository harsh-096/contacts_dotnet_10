"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "./api";

export type AsyncState<T> = {
  data: T | null;
  error: ApiError | Error | null;
  loading: boolean;
};

const initial: AsyncState<never> = { data: null, error: null, loading: true };

// Minimal hook that mirrors React Query's useQuery but without the dep.
// - revalidates on `key` change
// - exposes a manual `refresh()`
// - aborts in-flight requests on unmount / key change
export function useAsync<T>(
  key: unknown[],
  fetcher: (signal: AbortSignal) => Promise<T>,
  options?: { enabled?: boolean }
): AsyncState<T> & { refresh: () => void; setData: (v: T | null) => void } {
  const enabled = options?.enabled ?? true;
  const [state, setState] = useState<AsyncState<T>>(initial as AsyncState<T>);
  const [tick, setTick] = useState(0);
  const abortRef = useRef<AbortController | null>(null);

  const keyStr = JSON.stringify(key);

  useEffect(() => {
    if (!enabled) {
      setState({ data: null, error: null, loading: false });
      return;
    }

    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;

    setState((s) => ({ ...s, loading: true, error: null }));
    fetcher(ac.signal)
      .then((data) => {
        if (ac.signal.aborted) return;
        setState({ data, error: null, loading: false });
      })
      .catch((err) => {
        if (ac.signal.aborted) return;
        setState({ data: null, error: err, loading: false });
      });

    return () => ac.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [keyStr, tick, enabled]);

  const refresh = useCallback(() => setTick((n) => n + 1), []);
  const setData = useCallback((v: T | null) => setState((s) => ({ ...s, data: v })), []);

  return { ...state, refresh, setData };
}

// Wrap any async mutation into a state machine for buttons.
// `TInput` is `void` for parameterless mutations so callers can write
// `useMutation(() => SomeApi.remove(id))` and invoke `run()` with no args.
export function useMutation<TInput, TResult>(
  fn: (input: TInput) => Promise<TResult>
) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | Error | null>(null);

  const run = useCallback(
    async (input?: TInput): Promise<TResult | null> => {
      setLoading(true);
      setError(null);
      try {
        const result = await fn(input as TInput);
        return result;
      } catch (err) {
        setError(err as ApiError | Error);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [fn]
  );

  return { run, loading, error, setError };
}
