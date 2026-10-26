const styles = {
  primary: 'game-button-primary',
  secondary: 'game-button-secondary',
  danger: 'game-button-danger',
}

export default function Button({ as: Component = 'button', variant = 'primary', className = '', ...props }) {
  return (
    <Component
      className={`game-button inline-flex items-center justify-center gap-2 px-5 py-2.5 font-medium transition disabled:cursor-not-allowed disabled:opacity-50 ${styles[variant]} ${className}`}
      {...props}
    />
  )
}
