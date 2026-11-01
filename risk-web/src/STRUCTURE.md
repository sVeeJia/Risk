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
    spectate.js
    spectate.test.js
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
      HomeLayout.jsx
      SavedCampaigns.jsx
      savedCampaigns.js
      savedCampaigns.test.js
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
      GameLayout.jsx
      ShareLink.jsx
      SpectatorStatus.jsx
      spectatorEvents.js
      spectatorQueue.js
      spectatorSession.js
      spectator.test.js
      useSpectator.js
      replayState.js
      replayTimeline.js
      replayNavigation.js
      replay.fixture.js
      replay.test.js
      useReplay.js
      ReplayControls.jsx
      ReplayProgress.jsx
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
    Watch.jsx
    Replay.jsx
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
      saved-campaigns.css
      game-setup.css
      action-panel.css
      game-over.css
      event-log.css
      spectator.css
      replay.css
    pages/
      landing.css
      lobby.css
      login.css
      register.css
      game.css
```

- `main.jsx` initializes React, routing, authentication, and the sole stylesheet import; `App.jsx` defines routes.
- `api/` contains HTTP operations through the existing `/api` proxy. Owner requests use `client.js`; public spectator GETs explicitly omit credentials in `spectate.js`.
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


Public live viewing: `pages/Watch.jsx` serves `/watch/:gameId` without the login guard. `api/spectate.js` uses credential-free public GETs and the SignalR client (`@microsoft/signalr`) at `/hubs/game`. `spectate.test.js` checks URLs, cookie omission, cancellation and 404s. `GameLayout.jsx` shares the existing map, compact players list, overlays, dock and log with the owner's Game page. `SpectatorStatus.jsx` replaces action controls with read-only status and playback controls; `GameOver.jsx` omits restart actions in read-only mode. `ShareLink.jsx` uses `spectatorEvents.js` to build the public URL, with clipboard confirmation and a selectable fallback. Owner routes support both `/play/:gameId` and `/game/:gameId`. Finished share links point to `/replay/:gameId`; active-game links remain `/watch/:gameId`.

`spectatorEvents.js` merges event sequences, pages bare history arrays and rejects stale/unversioned snapshots. `spectatorSession.js` buffers the subscription/reconnection gap, rejoins the group and refetches missed events and the public snapshot. `spectatorQueue.js` serializes all action batches using the same `replayEvents` and `playbackPause` as NPC playback. `useSpectator.js` owns the cancellable connection lifetime, speed, battle presentation and live counts. `spectator.test.js` covers queue order, duplicates, paging, versions, catch-up races and cleanup. Styling is in `css/features/spectator.css`; dock dimensions are inherited unchanged. The local npm download cache is ignored in `risk-web/.gitignore`.

Browser verification uses the built frontend and the real SignalR client against an ephemeral mock HTTP/WebSocket hub, without starting or inspecting the backend. It covers anonymous history paging, live and buffered updates, fast-forward, battle visuals, refresh, owner read-only viewing, share fallback, game-over and 404 UI, including a zoom-equivalent narrow viewport.

Home screens: `HomeLayout.jsx` renders the shared header, hero, map preview and fixed three-slot action area. `GameMenu.jsx` supplies auth-specific header/actions and owns its existing briefing/setup dialog. Logged-in users have World conquest, Continue campaign and Review; logged-out users retain their existing two links with an invisible third slot to preserve alignment. The single-player menu has no friends/private-game entry. Short desktop viewports use a shared three-column hero/map/actions layout so the three stacked buttons fit without page scrolling. The former principles strip, footer, map legend and sea-route note and their styles/data have been removed. Both home pages use `css/features/game-menu.css`.

`SavedCampaigns.jsx` opens inside the existing dialog and uses `listGames` (`GET /api/games`), with cancellation, loading/empty/error/retry states. `savedCampaigns.js` sorts newest first, formats only server-provided round/player/result/country counts and optionally hydrates summary entries using the existing game GET. Its Node tests cover these displays, sorting, empty states and failed loading. The same panel takes `mode="resume" | "review"`: Continue campaign filters to active games and Review filters to finished games, newest first. Resume rows open `/play/:gameId`, matching game setup; finished rows show final rounds, result and country count with Watch replay actions to `/replay/:gameId`. Filter-specific empty messages distinguish no active campaigns from no finished games. Rows support focus and Enter/Space activation; narrow layouts put the action below the text. Human turns read "Your turn" and country labels use singular/plural correctly. Sharing is available only on the game page, not in this list. Panel styles live in `css/features/saved-campaigns.css`. The spectator Home link explicitly points to `/play` for authenticated users and `/` for guests.

Replay: `pages/Replay.jsx` loads the public DTO and every history page before mounting autoplay controls. `replayState.js` restores base armies/starting ownership and applies the explicit recorded-event bookkeeping contract transactionally, skipping incomplete old events. It compares the reconstructed final owners, soldiers, eliminated players and turn/result metadata with the DTO; differences or skipped events produce a non-blocking notice and console warning. `replayTimeline.js` precomputes immutable states, battle/arrow/decision presentation and event durations/markers. `replayNavigation.js` implements boundary seeking, timeline-time skips, speed math and non-overlapping three-lane flag placement with connectors to exact conquest times. `replay.fixture.js` and `replay.test.js` cover a full game and exact final-state equality, partial histories, metadata, markers, timing and seeking.

`useReplay.js` owns cancellable pacing and keyboard navigation; `ReplayControls.jsx` and `ReplayProgress.jsx` render the transport, speeds and accessible scrubber in the existing fixed dock. `css/features/replay.css` scales animations at x1/x2/x4, disables them during seeking and at x8/x16, and respects reduced motion. `GameLayout.jsx` accepts replay controls and a non-blocking result overlay without replacing the dock on finish; `EventLog.jsx` adds optional event seeking and current-event scrolling while preserving live-log behavior. The home Review entry opens the finished-games panel; only its Watch replay rows open replay in-app. Finished watch links redirect with replacement; ongoing replays provide a watch-live link. No backend files are involved.

Replay browser checks use the built app against mock public API responses with multiple history pages. They cover Review autoplay, all transports/speeds, scrub/flag/log navigation, static seek visuals, NPC Why cards, completed-watch replacement, context-sensitive sharing, missing/empty/incomplete histories and fixed dock/map geometry at desktop/mobile and zoom-equivalent viewport sizes.
