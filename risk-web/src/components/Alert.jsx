export default function Alert({ children }) {
  if (!children) return null
  return (
    <p role="alert" className="rounded-lg border border-red-900/60 bg-red-950/40 px-3.5 py-2.5 text-sm text-red-300">
      {children}
    </p>
  )
}