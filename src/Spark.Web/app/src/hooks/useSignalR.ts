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
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.onreconnecting(() => setIsConnected(false))
    connection.onreconnected(() => setIsConnected(true))
    connection.onclose(() => setIsConnected(false))

    // Register event handlers
    if (options.onMessage) {
      connection.on('UpdateProgress', options.onMessage)
    }
    if (options.onError) {
      connection.on('Error', options.onError)
    }

    connection
      .start()
      .then(() => {
        setIsConnected(true)
        setAuthError(false)
        connectionRef.current = connection
      })
      .catch((err) => {
        console.error('SignalR connection error:', err)
        // Check for authentication/authorization errors
        if (err.message?.includes('401') || err.message?.includes('403') || 
            err.message?.includes('Unauthorized') || err.message?.includes('Forbidden')) {
          setAuthError(true)
          options.onAuthError?.()
          options.onError?.('Authentication required. Please log in as an admin.')
        } else {
          options.onError?.(err.message)
        }
      })

    return () => {
      connection.stop()
    }
  }, [hubUrl])

  return {
    connection: connectionRef.current,
    isConnected,
    authError,
  }
}
