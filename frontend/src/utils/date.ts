const dateFormatter = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  timeZone: 'UTC',
})

function parseIsoDate(value: string): Date {
  const [year = 0, month = 1, day = 1] = value.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1, day))
}

export function formatDateRange(start: string, end: string): string {
  return dateFormatter.formatRange(parseIsoDate(start), parseIsoDate(end)).replace(/\s+/g, ' ')
}

const dayLabelFormatter = new Intl.DateTimeFormat('en-US', {
  weekday: 'short',
  month: 'short',
  day: 'numeric',
  timeZone: 'UTC',
})

export function formatDayLabel(date: string): string {
  return dayLabelFormatter.format(parseIsoDate(date))
}

const monthFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', timeZone: 'UTC' })
const weekdayFormatter = new Intl.DateTimeFormat('en-US', { weekday: 'short', timeZone: 'UTC' })

export interface DateParts {
  month: string
  day: number
  weekday: string
}

export function dateParts(date: string): DateParts {
  const parsed = parseIsoDate(date)
  return {
    month: monthFormatter.format(parsed),
    day: parsed.getUTCDate(),
    weekday: weekdayFormatter.format(parsed),
  }
}

const millisecondsPerDay = 24 * 60 * 60 * 1000

export function countDays(start: string, end: string): number {
  return (parseIsoDate(end).getTime() - parseIsoDate(start).getTime()) / millisecondsPerDay + 1
}
