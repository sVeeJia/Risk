export class ApiError extends Error {
  constructor(status, message, fieldErrors = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

function toApiError(status, data) {
  if (status === 429) {
    return new ApiError(status, 'Too many attempts. Wait a minute and try again.')
  }

  if (data?.errors) {
    const fieldErrors = {}
    Object.entries(data.errors).forEach(([key, messages]) => {
      const name = key.replace(/^\$\./, '').toLowerCase()
      fieldErrors[name] = Array.isArray(messages) ? messages[0] : String(messages)
    })
    const first = Object.values(fieldErrors)[0]
    return new ApiError(status, first ?? 'Check the highlighted fields.', fieldErrors)
  }

  return new ApiError(status, data?.message ?? 'Something went wrong. Try again.')
}

async function request(method, path, body, options = {}) {
  let response
  try {
    response = await fetch(path, {
      method,
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
      credentials: 'same-origin',
      signal: options.signal,
    })
  } catch (error) {
    if (error.name === 'AbortError') throw error
    throw new ApiError(0, "Can't reach the server. Check your connection and try again.")
  }

  if (response.status === 204) return null

  const data = await response.json().catch(() => null)
  if (response.ok) return data
  throw toApiError(response.status, data)
}

export const api = {
  get: (path, options) => request('GET', path, undefined, options),
  post: (path, body) => request('POST', path, body),
}
