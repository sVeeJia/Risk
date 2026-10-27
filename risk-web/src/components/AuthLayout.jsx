import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Shield } from 'lucide-react'
import WorldMap from '../features/map/WorldMap.jsx'

export default function AuthLayout({ title, subtitle, footer, children }) {
  return (
    <main className="auth-scene">
      <div className="auth-map"><WorldMap /></div>
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3 }}
        className="w-full max-w-md"
      >
        <Link to="/" className="mb-8 flex items-center justify-center gap-2 text-slate-300 hover:text-white">
          <Shield className="size-6 text-[#d7b374]" aria-hidden="true" />
          <span className="text-lg font-semibold tracking-wide">Geopolitics</span>
        </Link>

        <div className="auth-panel">
          <h1 className="text-2xl font-semibold text-white">{title}</h1>
          <p className="mt-1 text-sm text-slate-400">{subtitle}</p>
          <div className="mt-6">{children}</div>
        </div>

        <p className="mt-6 text-center text-sm text-slate-400">{footer}</p>
      </motion.div>
    </main>
  )
}
