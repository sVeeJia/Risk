import { useState } from 'react'
import { Link } from 'react-router-dom'
import AuthLayout from '../components/AuthLayout.jsx'
import Alert from '../components/Alert.jsx'
import Button from '../components/Button.jsx'
import Field from '../components/Field.jsx'
import { useAuth } from '../auth/useAuth.js'

export default function Login() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await login(email, password) // on success the route guard sends us to /play
    } catch (err) {
      setError(err.message)
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout
      title="Welcome back"
      subtitle="Log in to continue your campaign."
      footer={<>New here? <Link to="/register" className="text-emerald-400 hover:underline">Create an account</Link></>}
    >
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <Alert>{error}</Alert>
        <Field
          id="email" label="Email" type="email" autoComplete="email" required maxLength={254}
          placeholder="you@example.com" value={email} onChange={(e) => setEmail(e.target.value)}
        />
        <Field
          id="password" label="Password" type="password" autoComplete="current-password" required maxLength={128}
          value={password} onChange={(e) => setPassword(e.target.value)}
        />
        <Button type="submit" className="w-full" disabled={submitting}>
          {submitting ? 'Logging in…' : 'Login'}
        </Button>
      </form>
    </AuthLayout>
  )
}