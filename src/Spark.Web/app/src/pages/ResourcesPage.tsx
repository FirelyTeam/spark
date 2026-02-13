import { useEffect, useState, useRef } from 'react'
import {
  Cell,
  Column,
  Row,
  Table,
  TableBody,
  TableHeader,
  SearchField,
  Input,
  Label,
  Button,
} from 'react-aria-components'
import { useCapability } from '../hooks/useCapability'

interface ResourceCount {
  resourceType: string
  count: number | null
}

// Cache for resource counts
const countCache = new Map<string, number>()

export function ResourcesPage() {
  const { capability, loading: capabilityLoading, error } = useCapability()
  const [resources, setResources] = useState<ResourceCount[]>([])
  const [filter, setFilter] = useState('')
  const fetchedRef = useRef(false)

  useEffect(() => {
    if (!capability || fetchedRef.current) return
    
    const rest = capability.rest?.[0]
    const resourceTypes: ResourceCount[] = rest?.resource?.map((r) => ({
      resourceType: r.type,
      count: countCache.get(r.type) ?? null,
    })) ?? []
    
    setResources(resourceTypes)
    fetchedRef.current = true
    
    // Fetch counts only for resources not in cache
    const uncachedTypes = resourceTypes.filter(r => r.count === null)
    
    // Batch fetch in groups of 10 to avoid overwhelming the server
    const batchSize = 10
    const batches: ResourceCount[][] = []
    for (let i = 0; i < uncachedTypes.length; i += batchSize) {
      batches.push(uncachedTypes.slice(i, i + batchSize))
    }
    
    batches.forEach((batch, batchIndex) => {
      setTimeout(() => {
        batch.forEach((r) => {
          fetch(`/fhir/${r.resourceType}?_summary=count`, {
            headers: { Accept: 'application/fhir+json' },
          })
            .then((res) => res.json())
            .then((bundle) => {
              const count = bundle.total ?? 0
              countCache.set(r.resourceType, count)
              setResources((prev) =>
                prev.map((p) =>
                  p.resourceType === r.resourceType
                    ? { ...p, count }
                    : p
                )
              )
            })
            .catch(() => {
              // Ignore count errors, leave as null
            })
        })
      }, batchIndex * 200) // Stagger batches by 200ms
    })
  }, [capability])

  const filteredResources = resources.filter((r) =>
    r.resourceType.toLowerCase().includes(filter.toLowerCase())
  )

  if (capabilityLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-spark-cyan" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-red-900/20 border border-red-500/30 rounded-lg p-4">
        <p className="text-red-400">Error: {error}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white">Resources</h1>
          <p className="mt-1 text-gray-400">
            {resources.length} resource types supported
          </p>
        </div>
      </div>

      <SearchField className="max-w-md" value={filter} onChange={setFilter}>
        <Label className="sr-only">Search resources</Label>
        <div className="relative">
          <svg
            className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
          <Input
            placeholder="Search resource types..."
            className="w-full pl-10 pr-4 py-2 bg-spark-surface border border-spark-border rounded-lg text-white placeholder-gray-500 focus:ring-2 focus:ring-spark-cyan focus:border-transparent"
          />
        </div>
        {filter && (
          <Button
            onPress={() => setFilter('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-300"
          >
            ✕
          </Button>
        )}
      </SearchField>

      <div className="bg-spark-surface border border-spark-border rounded-lg overflow-hidden">
        <Table
          aria-label="Resource types"
          className="min-w-full divide-y divide-spark-border"
        >
          <TableHeader className="bg-spark-dark">
            <Column isRowHeader className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">
              Resource Type
            </Column>
            <Column className="px-6 py-3 text-right text-xs font-medium text-gray-400 uppercase tracking-wider">
              Count
            </Column>
          </TableHeader>
          <TableBody className="divide-y divide-spark-border">
            {filteredResources.map((resource) => (
              <Row key={resource.resourceType} className="hover:bg-spark-dark/50">
                <Cell className="px-6 py-4 whitespace-nowrap">
                  <a
                    href={`/fhir/${resource.resourceType}`}
                    className="text-sm font-medium text-gray-200 hover:text-spark-cyan"
                  >
                    {resource.resourceType}
                  </a>
                </Cell>
                <Cell className="px-6 py-4 whitespace-nowrap text-right">
                  {resource.count === null ? (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-spark-border text-gray-500">
                      ...
                    </span>
                  ) : (
                    <a
                      href={`/fhir/${resource.resourceType}`}
                      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        resource.count > 0
                          ? 'bg-spark-cyan/20 text-spark-cyan hover:bg-spark-cyan/30'
                          : 'bg-spark-border text-gray-500'
                      }`}
                    >
                      {resource.count.toLocaleString()}
                    </a>
                  )}
                </Cell>
              </Row>
            ))}
          </TableBody>
        </Table>

        {filteredResources.length === 0 && (
          <div className="text-center py-12 text-gray-500">
            No resources match "{filter}"
          </div>
        )}
      </div>
    </div>
  )
}
