import { useState, useCallback } from 'react'
import { Button } from 'react-aria-components'
import { useSignalR } from '../hooks/useSignalR'

type MaintenanceOperation = 'ClearStore' | 'LoadExamplesToStore' | 'RebuildIndex' | null

const operationLabels: Record<Exclude<MaintenanceOperation, null>, string> = {
  ClearStore: 'clear',
  LoadExamplesToStore: 'load',
  RebuildIndex: 'reindex',
}

export function AdminPage() {
  const [operation, setOperation] = useState<MaintenanceOperation>(null)
  const [messages, setMessages] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)

  const { connection, isConnected } = useSignalR('/maintenanceHub', {
    onMessage: useCallback((msg: string) => {
      setMessages(prev => [...prev.slice(-100), msg])
      if (msg.toLowerCase().includes('finished') || msg.toLowerCase().includes('cleared') || msg.toLowerCase().includes('rebuilt')) {
        setOperation(null)
      }
    }, []),
    onError: useCallback((msg: string) => {
      setError(msg)
      setOperation(null)
    }, []),
  })

  const startOperation = async (op: MaintenanceOperation) => {
    if (!connection || !op) return

    setOperation(op)
    setMessages(prev => [...prev, `\n--- ${operationLabels[op].toUpperCase()} ---`])
    setError(null)

    try {
      await connection.invoke(op)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
      setOperation(null)
    }
  }

  const clearLog = () => setMessages([])

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-white">Administration</h1>
        <p className="mt-2 text-gray-400">
          Database maintenance and management operations
        </p>
      </div>

      <div className="bg-spark-surface border border-spark-border rounded-lg p-6">
        <div className="flex items-center gap-2 mb-6">
          <div
            className={`w-2 h-2 rounded-full ${
              isConnected ? 'bg-green-500' : 'bg-red-500'
            }`}
          />
          <span className="text-sm text-gray-400">
            {isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>

        {error && (
          <div className="mb-6 bg-red-900/20 border border-red-500/30 rounded-lg p-4">
            <p className="text-red-400">{error}</p>
          </div>
        )}

        {messages.length > 0 && (
          <div className="mb-6 space-y-2">
            <div className="flex items-center justify-between">
              {operation ? (
                <div className="flex items-center gap-2">
                  <div className="animate-spin rounded-full h-4 w-4 border-2 border-spark-cyan border-t-transparent" />
                  <span className="font-medium text-white capitalize">{operationLabels[operation]}</span>
                </div>
              ) : (
                <span className="text-sm text-gray-400">Log</span>
              )}
              <Button
                onPress={clearLog}
                className="text-xs text-gray-500 hover:text-gray-300"
              >
                Clear
              </Button>
            </div>
            <div className="bg-spark-dark rounded-lg p-3 max-h-60 overflow-y-auto font-mono text-xs">
              {messages.map((msg, i) => (
                <div 
                  key={i} 
                  className={
                    msg.startsWith('\n---') 
                      ? 'text-spark-cyan font-bold mt-2 first:mt-0' 
                      : msg.toLowerCase().includes('error') 
                        ? 'text-red-400' 
                        : 'text-gray-400'
                  }
                >
                  {msg}
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="grid gap-4 md:grid-cols-3">
          <OperationCard
            title="Clear Database"
            description="Remove all resources from the database"
            variant="danger"
            disabled={!!operation || !isConnected}
            onPress={() => startOperation('ClearStore')}
          />
          <OperationCard
            title="Load Examples"
            description="Load FHIR example resources"
            variant="primary"
            disabled={!!operation || !isConnected}
            onPress={() => startOperation('LoadExamplesToStore')}
          />
          <OperationCard
            title="Reindex"
            description="Rebuild search indexes"
            variant="secondary"
            disabled={!!operation || !isConnected}
            onPress={() => startOperation('RebuildIndex')}
          />
        </div>
      </div>

      <div className="bg-amber-900/20 border border-amber-500/30 rounded-lg p-4">
        <h3 className="font-medium text-amber-400">Warning</h3>
        <p className="mt-1 text-sm text-amber-300/80">
          These operations can affect all data in the database. Use with caution in production environments.
        </p>
      </div>
    </div>
  )
}

function OperationCard({
  title,
  description,
  variant,
  disabled,
  onPress,
}: {
  title: string
  description: string
  variant: 'primary' | 'secondary' | 'danger'
  disabled: boolean
  onPress: () => void
}) {
  const colors = {
    primary: 'bg-spark-cyan hover:bg-spark-sky text-spark-navy font-semibold',
    secondary: 'bg-spark-border hover:bg-spark-border/80 text-white',
    danger: 'bg-red-600 hover:bg-red-700 text-white',
  }

  return (
    <div className="border border-spark-border rounded-lg p-4 bg-spark-dark flex flex-col h-full">
      <div className="flex-grow">
        <h3 className="font-semibold text-white">{title}</h3>
        <p className="text-sm text-gray-400">{description}</p>
      </div>
      <Button
        onPress={onPress}
        isDisabled={disabled}
        className={`w-full py-2 px-4 rounded-md font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed mt-3 ${colors[variant]}`}
      >
        {title}
      </Button>
    </div>
  )
}
