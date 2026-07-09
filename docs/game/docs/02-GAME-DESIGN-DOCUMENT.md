# Nexo World — Game Design Document

## Product overview
Nexo World turns technical systems and AI-assisted work into a navigable city. It uses readable third-person urban controls while every system supports investigation, learning, collaboration, and visible progress.

## World metaphor
| Digital concept | World representation |
|---|---|
| Project | district, building, work site, or garden |
| Task | job-board item, room activity, or repair site |
| AI agent | citizen, specialist, dispatcher, or engineer |
| MCP/tool integration | shop, kiosk, or service counter |
| Permission | gate, badge, or access desk |
| Evidence | physical clue, telemetry card, or terminal result |
| Incident | outage, blocked road, dark building, stalled service |
| Validation | restored signal, test panel, and live city reaction |
| AAR/report | operations review room and evidence board |

## Player
The player is an Operator assigned to Meridian District. The approved current player character is frozen. Preserve walking, sprinting, jumping, rotation, camera, input, and collision unless a measured defect requires a focused fix.

## Core modes
`exploration`, `conversation`, `investigation`, `vehicle`, `interaction`, `decision`, `validation`, `aar`, `paused`.
Only one mode owns primary input at a time.

## Interaction
Interactables register a marker and radius. The closest valid object in the forward cone becomes focused and exposes a short prompt such as `E — Talk to Maya`, `E — Inspect telemetry`, or `E — Enter vehicle`.

## Dialogue
Each agent has a role, location, concern, known evidence, unknown evidence, confidence state, requests, and mission-state reactions. Dialogue is short and actionable rather than a long chatbot monologue.

## Evidence
Evidence includes `id`, `source`, `category`, `summary`, `details`, `reliability`, `timestamp`, `relatedEntityIds`, `visibleToPlayer`, and `usedInDecision`. Categories: symptom, telemetry, configuration, deployment, dependency, testimony, validation.

## Decisions
Each intervention defines required evidence, optional evidence, risk, expected effect, failure effect, validation, and reversibility. The first mission contains one unsafe shortcut, one incomplete temporary fix, and one evidence-supported safe resolution.

## World state
Mission outcomes change traffic lights, street lighting, shop signs, pedestrian behavior, blocked paths, building access, ambient sound, agent dialogue, and mission-board state.

## Vehicle
One hero sedan must support driver-side approach, contextual prompt, door sequence, player alignment, driving, chase camera, collision, safe low-speed exit, blocked-exit handling, and restoration of the unchanged player.

## Pedestrians
Target 8–16 active nearby pedestrians and 10–30 distant simplified pedestrians. Use sidewalk routes, crossings, personal spacing, role destinations, idle variation, distance-based animation, and vehicle awareness.

## Mission pattern
Hook → owner → symptoms → evidence → hypothesis → intervention → validation → visible reaction → AAR.

## Scoring
- evidence quality: 30%
- process safety: 25%
- technical decision: 25%
- validation quality: 15%
- unnecessary disruption: 5%

## Failure
Failure is recoverable. Wrong actions can worsen the district, require rollback, or require more evidence, but should not produce a hard game-over.

## UI
Keep the screen mostly world-first. Persistent UI is limited to the objective and focused prompt. Evidence, dialogue, decisions, and AAR are contextual.

## Accessibility
Remappable actions in data, subtitles, high-contrast prompts, reduced camera motion, camera sensitivity, sprint toggle, and no information conveyed by color alone.
