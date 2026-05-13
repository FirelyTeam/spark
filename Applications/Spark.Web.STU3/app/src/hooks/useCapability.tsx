import { useEffect, useState } from 'react'

interface CapabilityStatement {
  resourceType: string
  fhirVersion: string
  software?: {
    name: string
    version: string
  }
  implementation?: {
    description: string
    url: string
  }
  rest?: Array<{
    resource?: Array<{
      type: string
      interaction?: Array<{ code: string }>
      searchParam?: Array<{ name: string; type: string }>
    }>
  }>
}

// Simple in-memory cache
let cachedCapability: CapabilityStatement | null = null
let cacheTimestamp: number = 0
const CACHE_DURATION = 60000 // 1 minute

const isCacheValid = () =>
  cachedCapability !== null && Date.now() - cacheTimestamp < CACHE_DURATION

export function useCapability() {
  const [capability, setCapability] = useState<CapabilityStatement | null>(
    isCacheValid() ? cachedCapability : null
  )
  const [loading, setLoading] = useState(!isCacheValid())
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isCacheValid()) {
      return
    }

    fetch('/fhir/metadata', {
      headers: { Accept: 'application/fhir+json' },
    })
      .then((res) => {
        if (!res.ok) throw new Error('Failed to fetch metadata')
        return res.json()
      })
      .then((data) => {
        cachedCapability = data
        cacheTimestamp = Date.now()
        setCapability(data)
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return { capability, loading, error }
}
