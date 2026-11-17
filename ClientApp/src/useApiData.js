import { useEffect, useState } from "react";
import { api } from "./api";

export function useApiData(url, pollMs = 0) {
  const [result, setResult] = useState(null);
  const [failure, setFailure] = useState(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    if (!url) return;

    const controller = new AbortController();
    let busy = false;

    async function load() {
      if (busy) return;
      busy = true;

      try {
        const data = await api(url, { signal: controller.signal });

        if (!controller.signal.aborted) {
          setResult({ url, data });
          setFailure(null);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setFailure({ url, message: error.message });
        }
      } finally {
        busy = false;
      }
    }

    load();
    const timer = pollMs ? window.setInterval(load, pollMs) : null;

    return () => {
      controller.abort();
      if (timer !== null) window.clearInterval(timer);
    };
  }, [url, pollMs, revision]);

  return {
    data: result?.url === url ? result.data : null,
    error: failure?.url === url ? failure.message : "",
    reload: () => setRevision((value) => value + 1),
  };
}
