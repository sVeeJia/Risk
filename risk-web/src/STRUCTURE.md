# Frontend structure

```text
src/
  main.jsx
  App.jsx
  api/
    client.js
    countries.js
  auth/
    authContext.js
    AuthProvider.jsx
    useAuth.js
  routes/
    guards.jsx
    RequireAuth.jsx
    RedirectIfAuthed.jsx
  components/
    Alert.jsx
    AuthLayout.jsx
    Button.jsx
    Field.jsx
    Splash.jsx
  features/
    countries/
      CountryPicker.jsx
      filterCountries.js
      filterCountries.test.js
    game/
      GameMenu.jsx
      GameSetup.jsx
      boardAssignments.js
      boardAssignments.test.js
    map/
      WorldMap.jsx
      worldRegions.js
  pages/
    Landing.jsx
    Lobby.jsx
    Game.jsx
    Login.jsx
    Register.jsx
  css/
    index.css
    tokens.css
    base.css
    components/
      alert.css
      auth-layout.css
      button.css
      field.css
      splash.css
    features/
      country-picker.css
      world-map.css
      game-menu.css
      game-setup.css
    pages/
      landing.css
      lobby.css
      login.css
      register.css
      game.css
```

- `main.jsx` initializes React, routing, authentication, and the sole stylesheet import; `App.jsx` defines routes.
- `api/` contains HTTP operations; every request goes through `client.js` and the existing `/api` proxy.
- `auth/` contains the authentication context, provider, and hook.
- `routes/` contains route guards; `guards.jsx` re-exports their default components as the existing named interface.
- `components/` contains reusable presentation components.
- `features/` contains domain-specific components, helpers, data, and tests.
- `features/countries/` owns country selection and pure filtering, sorting, and pagination.
- `features/game/` owns the game menu, setup, and mapping server territory slots onto map shapes.
- `features/map/` owns map rendering and territory data.
- `pages/` contains route-level page components.
- `css/` contains the stylesheet entrypoint, existing tokens, and shared document/dialog rules.
- `css/components/` contains shared component styles.
- `css/features/` contains feature styles.
- `css/pages/` reserves a stylesheet for each page; existing utility styling stays in JSX.

all CSS lives in src/css/.

Only `main.jsx` imports `css/index.css`. That stylesheet imports Tailwind first, followed by tokens, base, components, features, and pages. Shared dialog typography stays in `base.css` so feature overrides retain their existing precedence. No new tokens are introduced.

Verification: `npm run lint`, `npm run build`, and `npm test`.
