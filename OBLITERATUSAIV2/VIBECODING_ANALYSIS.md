# Vibecoding City — Concept Analysis

Analysis only; nothing here is implemented yet. How the Unity city becomes a
vibecoding environment: the city is the IDE, buildings are projects, shops are
tools, citizens are agents. This is the original North Star
("AI agents live as citizens, tools become shops, projects become districts")
made concrete against the systems that now exist in `UnityProject`.

## 1. Concept → system mapping

| Concept | World representation | Existing system it builds on |
|---|---|---|
| Folder / repo | Building (height = code size, variant = language, sign = name) | `LegacyCityPortBuilder` building pipeline, sign/texture baking |
| Parent folder / org | District (a block or group of blocks) | `CityLayout` block grid |
| Tool (git, npm, Claude Code, MCP server) | Shop on a marketplace street | `IInteractable` + `PlayerInteractor`, storefront materials |
| AI agent / running job | Citizen who walks to the project's building | `PedestrianSystem` (+ AI Navigation package, installed and unused) |
| Build/agent job in progress | Construction overlay: crane, scaffold, cones | Construction site kit (already built) |
| Project status (CI, TODOs) | Billboards and building lights | Billboard texture baking, emissive materials |
| Incident (test failure, outage) | Physical disruption: blocked road, flickering signals, smoke | `TrafficSignalNetwork`, traffic system |
| Fast travel | Player car + home garage | `ArcadeVehicleController`, `VehicleGarageReturn` |
| Reading code | Door interaction → README / file tree / diff panels | `IInteractable`; UI Toolkit for panels |
| Writing code | You don't type — you prompt; an agent (citizen) does the work | Process-spawned CLI (e.g. `claude -p`), matches pillar #4 |

Key design position: **no embedded text editor.** A full editor inside Unity is
a huge lift and contradicts pillar #4 ("AI agents are collaborators, not chat
windows"). Reading happens in-world through panels; writing happens by
dispatching agents. You direct; citizens type.

## 2. Architecture (five layers, each independently shippable)

### Layer 1 — Workspace mirror (read-only world generation)
- `WorkspaceScanner` (runtime, System.IO): scan a configured root folder; per
  project collect name, file count, rough LOC, last-modified, git metadata
  (shell out to `git log -1` or parse `.git/HEAD`).
- `ProjectCityDataSource`: deterministic mapping project → lot. Use a stable
  hash of the project name so **buildings never move between sessions** —
  spatial memory is the entire point of the metaphor. Height = log(LOC),
  facade variant = dominant language, construction state = recent activity.
- The existing builder pipeline consumes these specs instead of seeded random.
- Signs: TextMeshPro on quads at runtime (simplest), baked textures for
  distant/large signage.

### Layer 2 — Inspection
- One door interactable per building → UI Toolkit panel: README (plain text
  first, markdown later), file tree, recent commits.
- Interiors (lobby scene, floors = folders) are a stretch goal; panels deliver
  90% of the value for 10% of the cost.

### Layer 3 — Tool marketplace
- Tool registry: a static core list (git, npm, test runner, Claude Code) plus
  **MCP discovery** — parse `~/.claude.json` / project `.mcp.json`; every
  configured MCP server automatically gets a shop. The marketplace literally
  reflects your real tool setup.
- Shop interaction: description, per-project enabled state, "hire for next
  job."

### Layer 4 — Agent dispatch (the vibecoding loop)
- `AgentJobRunner`: `System.Diagnostics.Process` running
  `claude -p "<prompt>" --output-format stream-json` (or any CLI) with
  cwd = the project's folder; stream stdout on a background thread, marshal
  status to the main thread.
- World feedback: a citizen spawns and walks to the building (this is where
  the AI Navigation package finally earns its place — the loop-walking
  pedestrians stay as ambience, dispatched agents navigate); construction
  overlay while running; diff-summary panel on completion; building visibly
  changes (new floor, fresh facade).
- The whole loop = the old core loop verbatim: discover problem → talk to
  agent → gather evidence → visit tools → intervene → validate.

### Layer 5 — Incidents & missions
- CI/test failures become physical events: the ported traffic-signal network
  makes the old "Meridian Signal Failure" vertical-slice mission literally
  implementable — a failing service flickers the signals in its district
  until an agent job fixes it.

## 3. Feasibility notes (Unity 6, macOS, Mono)

- **File IO / processes:** full .NET is available; `System.IO` and
  `Process` work in editor and macOS standalone. Two gotchas: standalone apps
  launched from Finder get a minimal `PATH` (resolve `claude`/`git` by
  absolute path or login-shell lookup), and editor domain reloads kill child
  processes (track and re-attach/kill jobs).
- **Live updates:** `FileSystemWatcher` for cheap re-scan triggers; rebuild a
  single building on change, never the whole city.
- **UI:** UI Toolkit for panels, TextMeshPro for world text. No new paid
  dependencies needed.
- **Multiplayer later:** city layout is a pure function of workspace state +
  deterministic hashing, so it replicates the same way the seeded systems do;
  agent jobs become networked events. The "simulation state in plain
  components" discipline we kept pays off here.
- **Security (the serious one):** agent jobs edit real files. Defaults must
  be: allowlisted workspace root only, read-only inspection without
  confirmation, explicit per-job confirmation with the exact prompt and
  target shown, and never auto-dispatch. A "demolition" (file deletion)
  should be impossible from the game.

## 4. What survives from the current game

Everything. Traffic/pedestrians/clouds/terrain are the ambience that makes
the workspace feel alive; the car and garage are fast travel; construction is
job visualization; billboards are dashboards; signals are ops events. The
city-life port was not a detour — it is the interface.

## 5. Suggested phasing (when implementation starts)

1. **P1 Workspace mirror** — real folders → named buildings + door panel
   with README/file tree. Smallest slice that proves the concept.
2. **P2 Marketplace** — static tool registry + MCP server discovery.
3. **P3 Agent dispatch** — one job type end-to-end with citizen walk,
   construction viz, diff panel.
4. **P4 Incidents/missions** — signal-failure mission from the old docs.

## 6. Open questions

- Project lifecycle: what happens when a folder appears (vacant lot →
  construction) or disappears (condemned building? never silent deletion).
- Scale: 15 usable blocks today; a workspace with 100+ repos needs districts
  per parent folder or paging. Fine for P1 with top-level folders only.
- Where agent conversations live: in-world speech bubbles vs. a panel;
  panel first.
- Editor vs product: development happens in the Unity editor, but the real
  product is a standalone macOS build so the "IDE" isn't tethered to Unity.
