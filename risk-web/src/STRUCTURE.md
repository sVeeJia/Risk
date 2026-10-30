# Frontend structure

```text
src/
  main.jsx
  App.jsx
  api/
    client.js
    countries.js
    games.js
    games.test.js
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
      gameActions.js
      gameActions.test.js
      ActionPanel.jsx
      EventLog.jsx
      GameOver.jsx
      playbackSpeed.js
      playbackSpeed.test.js
      npcPlayback.js
      npcPlayback.test.js
      useNpcPlayback.js
    map/
      WorldMap.jsx
      Arrows.jsx
      arrows.js
      arrows.test.js
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
      map-arrows.css
      game-menu.css
      game-setup.css
      action-panel.css
      game-over.css
      event-log.css
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
- `features/game/` owns the game menu, setup, and mapping server territory slots onto map shapes. `gameActions.js` provides pure turn/selection helpers with Node tests; `ActionPanel.jsx` presents phase controls and `EventLog.jsx` displays server events and NPC justifications. Their styles live in `css/features/action-panel.css` and `css/features/event-log.css`.
- `api/games.js` wraps game queries and actions; `games.test.js` checks HTTP contracts, cancellation, and server errors. `pages/Game.jsx` owns server game state, event history, selection, and serialized requests.
- `features/map/` owns map rendering and territory data.
- `pages/` contains route-level page components.
- `css/` contains the stylesheet entrypoint, existing tokens, and shared document/dialog rules.
- `css/components/` contains shared component styles.
- `css/features/` contains feature styles.
- `css/pages/` reserves a stylesheet for each page; existing utility styling stays in JSX.

all CSS lives in src/css/.

`features/game/GameOver.jsx` presents server final results and links to a fresh setup or headquarters; its styles are in `css/features/game-over.css`. The `new=1` headquarters query opens the existing setup dialog.

NPC playback uses `useNpcPlayback.js` for cancellation, request ownership, errors and speed controls; `npcPlayback.js` sequences every server event before committing a turn. `playbackSpeed.js` defines both speeds and interruptible waits. Their Node tests cover event order, stopping, cancellation and speed changes. `map/arrows.js` maps server events to overlays and computes SVG geometry; `Arrows.jsx` renders those overlays using `css/features/map-arrows.css`. `arrows.test.js` covers event keys, geometry and retained attack arrows.

Only `main.jsx` imports `css/index.css`. That stylesheet imports Tailwind first, followed by tokens, base, components, features, and pages. Shared dialog typography stays in `base.css` so feature overrides retain their existing precedence. No new tokens are introduced.

Verification: `npm run lint`, `npm run build`, and `npm test`.
