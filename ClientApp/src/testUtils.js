// Builds the part of a fetch Response that the app reads, for mocking fetch.
export function jsonResponse(data, status = 200) {
  const text = data === undefined ? "" : JSON.stringify(data);

  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: async () => data,
    text: async () => text,
  });
}
