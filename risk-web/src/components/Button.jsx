const styles = {
  primary: 'game-button-primary',
  secondary: 'game-button-secondary',
  danger: 'game-button-danger',
}

export default function Button({ as: Component = 'button', variant = 'primary', size = 'regular', busy = false, disabled, className = '', ...props }) {
  return (
    <Component
      className={`game-button ${styles[variant]} game-button-${size} ${className}`}
      disabled={Component === 'button' ? disabled || busy : undefined}
      aria-busy={busy || undefined}
      {...props}
    />
  )
}
