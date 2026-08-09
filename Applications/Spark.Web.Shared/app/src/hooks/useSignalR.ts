import { useEffect, useState, useRef } from 'react'
import * as signalR from '@microsoft/signalr'

interface SignalROptions {
  onMessage?: (message: string) => void
  onError?: (message: string) => void
  onAuthError?: () => void
}

export function useSignalR(hubUrl: string, options: SignalROptions = {}) {
  const [isConnected, setIsConnected] = useState(false)
  const [authError, setAuthError] = useState(false)
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const optionsRef = useRef(options)

  useEffect(() => {
    optionsRef.current = options
  })

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    newConnection.onreconnecting(() => setIsConnected(false))
    newConnection.onreconnected(() => setIsConnected(true))
    newConnection.onclose(() => setIsConnected(false))

    // Register event handlers
    if (optionsRef.current.onMessage) {
      newConnection.on('UpdateProgress', (msg) => optionsRef.current.onMessage?.(msg))
    }
    if (optionsRef.current.onError) {
      newConnection.on('Error', (msg) => optionsRef.current.onError?.(msg))
    }

    newConnection
      .start()
      .then(() => {
        setIsConnected(true)
        setAuthError(false)
        setConnection(newConnection)
      })
      .catch((err) => {
        console.error('SignalR connection error:', err)
        // Check for authentication/authorization errors
        if (err.message?.includes('401') || err.message?.includes('403') || 
            err.message?.includes('Unauthorized') || err.message?.includes('Forbidden')) {
          setAuthError(true)
          optionsRef.current.onAuthError?.()
          optionsRef.current.onError?.('Authentication required. Please log in as an admin.')
        } else {
          optionsRef.current.onError?.(err.message)
        }
      })

    return () => {
      setConnection(null)
      newConnection.stop()
    }
  }, [hubUrl])

  return {
    connection,
    isConnected,
    authError,
  }
}
