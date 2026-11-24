import type { CreateTripInput, Trip } from '@/types/trip'

const baseUrl = '/api/trips'
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
  if (gatewayErrorStatuses.includes(response.status)) {
    return new ApiError(connectionErrorMessage, response.status)
  }

  const problem: ProblemDetails = await response.json().catch(() => ({}))
  const validationMessages = Object.values(problem.errors ?? {}).flat()
  const message =
    validationMessages.join(' ') ||
    problem.detail ||
    problem.title ||
    `Request failed with status ${response.status}`

  return new ApiError(message, response.status)
}

async function request(url: string, init?: RequestInit): Promise<Response> {
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

export async function fetchTrips(): Promise<Trip[]> {
  const response = await request(baseUrl)
  return response.json()
}

export async function createTrip(input: CreateTripInput): Promise<Trip> {
  const response = await request(baseUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return response.json()
}

export async function deleteTrip(id: string): Promise<void> {
  await request(`${baseUrl}/${id}`, { method: 'DELETE' })
}
