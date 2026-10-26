import { useState } from 'react'
import { Link } from 'react-router-dom'
import AuthLayout from '../components/AuthLayout.jsx'
import Alert from '../components/Alert.jsx'
import Button from '../components/Button.jsx'
import Field from '../components/Field.jsx'
import { useAuth } from '../auth/useAuth.js'

export default function Register() {
  const { register } = useAuth()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [fieldErrors, setFieldErrors] = useState({})
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')

    // Quick checks for a better experience. The server validates everything again.
    const problems = {}
    const name = displayName.trim()
    if (name.length < 2 || name.length > 40) problems.displayname = 'Display name must be 2 to 40 characters.'
    if (password.length < 10) problems.password = 'Use at least 10 characters.'
    if (password !== confirm) problems.confirm = "Passwords don't match."
    setFieldErrors(problems)
    if (Object.keys(problems).length > 0) return

    setSubmitting(true)
    try {
      await register(email, password, name) // on success the route guard sends us to /play
    } catch (err) {
      setFieldErrors(err.fieldErrors ?? {})
      setError(err.message)
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout
      title="Create your account"
      subtitle="Pick a name and start your first game."
      footer={<>Already have an account? <Link to="/login" className="text-emerald-400 hover:underline">Login</Link></>}
    >
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <Alert>{error}</Alert>
        <Field
          id="displayName" label="Display name" autoComplete="nickname" required maxLength={40}
          placeholder="Commander" value={displayName} onChange={(e) => setDisplayName(e.target.value)}
          error={fieldErrors.displayname}
        />
        <Field
          id="email" label="Email" type="email" autoComplete="email" required maxLength={254}
          placeholder="you@example.com" value={email} onChange={(e) => setEmail(e.target.value)}
          error={fieldErrors.email}
        />
        <Field
          id="password" label="Password" type="password" autoComplete="new-password" required maxLength={128}
          placeholder="At least 10 characters" value={password} onChange={(e) => setPassword(e.target.value)}
          error={fieldErrors.password}
        />
        <Field
          id="confirm" label="Confirm password" type="password" autoComplete="new-password" required maxLength={128}
          value={confirm} onChange={(e) => setConfirm(e.target.value)}
          error={fieldErrors.confirm}
        />
        <Button type="submit" className="w-full" disabled={submitting}>
          {submitting ? 'Creating account…' : 'Register'}
        </Button>
      </form>
    </AuthLayout>
  )
}