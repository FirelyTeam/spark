import { useCapability } from '../hooks/useCapability'

export function HomePage() {
  const { capability, loading, error } = useCapability()

  if (loading) {
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
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-white">
          {capability?.software?.name ?? 'Spark FHIR Server'}
        </h1>
        <p className="mt-2 text-gray-400">
          A public domain FHIR server implementation
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        <InfoCard
          title="FHIR Version"
          value={capability?.fhirVersion ?? 'Unknown'}
          icon={
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          }
        />
        <InfoCard
          title="Server Version"
          value={capability?.serverVersion ?? 'Unknown'}
          icon={
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
            </svg>
          }
        />
        <InfoCard
          title="Endpoint"
          value={capability?.implementation?.url ?? '/fhir'}
          icon={
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
            </svg>
          }
        />
      </div>

      <div className="bg-spark-surface border border-spark-border rounded-lg p-6">
        <h2 className="text-lg font-semibold text-white mb-4">Quick Links</h2>
        <div className="flex flex-wrap gap-3">
          <a
            href="/fhir/metadata"
            className="inline-flex items-center px-4 py-2 bg-spark-cyan text-spark-navy font-semibold rounded-md hover:bg-spark-sky transition-colors"
          >
            Capability Statement
          </a>
          <a
            href="https://www.hl7.org/fhir/"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center px-4 py-2 bg-spark-border text-gray-300 rounded-md hover:bg-spark-border/80 transition-colors"
          >
            FHIR Specification
          </a>
        </div>
      </div>
    </div>
  )
}

function InfoCard({ title, value, icon }: { title: string; value: string; icon: React.ReactNode }) {
  return (
    <div className="bg-spark-surface border border-spark-border rounded-lg p-6">
      <div className="flex items-center gap-4">
        <div className="p-3 bg-spark-cyan/20 text-spark-cyan rounded-lg">
          {icon}
        </div>
        <div>
          <p className="text-sm text-gray-400">{title}</p>
          <p className="text-lg font-semibold text-white truncate">{value}</p>
        </div>
      </div>
    </div>
  )
}
