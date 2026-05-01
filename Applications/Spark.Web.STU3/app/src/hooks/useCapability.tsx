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

export function useCapability() {
  const [capability, setCapability] = useState<CapabilityStatement | null>(cachedCapability)
  const [loading, setLoading] = useState(!cachedCapability)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const now = Date.now()
    
    // Use cache if still valid
    if (cachedCapability && now - cacheTimestamp < CACHE_DURATION) {
      setCapability(cachedCapability)
      setLoading(false)
      return
    }

    setLoading(true)
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
