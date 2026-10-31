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
      battle.js
      battlePresentation.js
      battle.test.js
      BattleBoard.jsx
      NpcDecisionCard.jsx
      Die.jsx
      playbackSpeed.js
      playbackSpeed.test.js
      npcPlayback.js
      npcPlayback.test.js
      useNpcPlayback.js
    map/
      WorldMap.jsx
      Arrows.jsx
      AttackMarkers.jsx
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
      attack-markers.css
      battle-board.css
      npc-decision-card.css
      die.css
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

Battle presentation: `features/game/battle.js` parses server dice and counts without applying game rules. `battlePresentation.js` retains visual attack context, the displayed round number and live count overrides; `battle.test.js` covers parsing, pairing and lifecycle resets. `BattleBoard.jsx` and `Die.jsx` render dice/results in the dock; `features/map/AttackMarkers.jsx` renders SVG badges, glows and brief event highlights inside the existing map area. Their styles live in `css/features/battle-board.css`, `die.css` and `attack-markers.css`. NPC callbacks clear live counts with each authoritative turn snapshot.

`NpcDecisionCard.jsx` retains the latest replayed NPC decision until turn end and renders it in the top-right map overlay; its presentation is in `css/features/npc-decision-card.css`. `BattleBoard.jsx` occupies the dock's wider left controls column, with status text above the right-hand controls. Its scalable SVG sheet labels the two aligned dice rows ATK/DEF in a single horizontal strip without scrolling; narrow phone layouts show summaries without scrolling the controls. The last roll stays until a new attack or turn begins. These presentation changes leave map/dock dimensions and replay timing unchanged.

The map card fits the `928 / 482` viewBox without vertical letterboxing, centred within `game-page-map-area`. The former decision-card top rail stays removed. The map takes the remaining height above a mode-independent compact dock: `--dock-height: clamp(256px, 27vh, 280px)` (phones retain a 420px stacked dock). The 256px minimum accommodates conquest transfer plus a one-line server error. Controls use fixed sizes without dock-height scaling, with their original action buttons pinned in a separate footer. The body stays vertically centred without internal scrolling. All action modes and final results share the `dock-body-group` layout: content blocks are centred horizontally and vertically, left-aligned internally, and separated by 36px when both are present. Empty board/status blocks do not reserve a column. A status-only block centres by itself; board appearance interpolates the group width and gap over 200ms, disabled for reduced motion. The board uses the existing dice-sheet width for its presentation width (at least 220px where space permits), anchors dice directly beside ATK/DEF labels, and keeps footer text inline. Status, selection and amount blocks share this placement before and after the first roll. Players remain compact and top-aligned. Reinforcement and action descriptions stay left of amount inputs; finished results and counts are also side by side. At high zoom only the page scrolls around the 240px minimum map row and fixed dock.

