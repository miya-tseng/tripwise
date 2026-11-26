const connectionErrorMessage = 'Cannot connect to the server.'
const gatewayErrorStatuses = [502, 503, 504]

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem: ProblemDetails = await response.json().catch(() => ({}))

  if (gatewayErrorStatuses.includes(response.status) && !problem.detail) {
    return new ApiError(connectionErrorMessage, response.status)
  }

  const validationMessages = Object.values(problem.errors ?? {}).flat()
  const message =
    validationMessages.join(' ') ||
    problem.detail ||
    problem.title ||
    `Request failed with status ${response.status}`

  return new ApiError(message, response.status)
}

export async function request(url: string, init?: RequestInit): Promise<Response> {
  let response: Response

  try {
    response = init ? await fetch(url, init) : await fetch(url)
  } catch {
    throw new ApiError(connectionErrorMessage, 0)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return response
}

export function jsonBody(method: string, body: unknown): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}
