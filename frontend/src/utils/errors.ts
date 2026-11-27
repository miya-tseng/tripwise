export const GENERIC_ERROR = 'Something went wrong.'

export function errorMessage(error: unknown, fallback = GENERIC_ERROR): string {
  return error instanceof Error ? error.message : fallback
}
