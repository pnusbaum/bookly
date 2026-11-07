import { useEffect, useState } from 'react'

function App() {
  const [status, setStatus] = useState('sprawdzam...')

  useEffect(() => {
    fetch('/api/health')
      .then((res) => (res.ok ? res.json() : Promise.reject(new Error(`HTTP ${res.status}`))))
      .then((data: { status: string }) => setStatus(data.status))
      .catch(() => setStatus('brak połączenia z API'))
  }, [])

  return (
    <main>
      <h1>Bookly</h1>
      <p>API: {status}</p>
    </main>
  )
}

export default App