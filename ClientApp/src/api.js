export async function api(path, options = {}) {
  const response = await fetch(path, {
    credentials: "same-origin",
    ...options,
  });

  const text = await response.text();
  let data = null;

  try {
    data = text ? JSON.parse(text) : null;
  } catch {}

  if (!response.ok) {
    const validationMessage = data?.errors
      ? Object.values(data.errors).flat().join(" ")
      : null;

    const message =
      response.status === 401
        ? "Please log in again."
        : response.status === 403
          ? "You do not have permission to do this."
          : data?.message ||
            validationMessage ||
            data?.title ||
            `Request failed (${response.status}).`;

    const error = new Error(message);
    error.status = response.status;
    throw error;
  }

  return data;
}

export function postJson(path, body) {
  return api(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}
