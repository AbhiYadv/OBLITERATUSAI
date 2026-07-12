# Initial Gameplay Review

Date: 2026-07-11

Scope: compare the current 300 m Unity prototype with the design qualities of
GTA-style urban open worlds. This is a prototype review, not a recommendation
to match AAA scope or copy GTA content.

## Current Baseline

- 300 m by 300 m city plateau: 16 equal blocks on a five-by-five street grid.
- 47 buildings, one construction site, one external garage, and four billboards.
- 40 streetlights, 100 signal heads, 14 parked cars, 147 street trees, 64
  benches, 45 bins, 384 bollards, and nine cafe umbrellas.
- Runtime ambience: 24 moving vehicles, 28 pedestrians, clouds, terrain,
  distant forest, river, and tree wind.
- Playable systems: third-person walking, sprint, jump, camera, one driveable
  sedan, vehicle entry/exit, and abandoned-car return.
- Only one meaningful scene interaction currently exists: entering the sedan.
- No objective, mission, minimap, audio layer, health, police response,
  inventory, shops, pedestrian destinations, or world save.

## Comparison

| Area | Current prototype | GTA-style expectation | Assessment |
| --- | --- | --- | --- |
| Map readability | Clear grid, central blue tower, construction landmark | District silhouettes, arterial hierarchy, landmarks, shortcuts | Readable but repetitive |
| District identity | Same facade and sidewalk grammar across all blocks | Neighborhood-specific architecture, color, traffic, props, and activities | Major gap |
| Traversal | Basic walking and one garage sedan | Responsive movement, strong camera feedback, varied routes and destinations | Functional, not expressive |
| Traffic | 24 deterministic cars on 12 rectangular loops | Road-dependent density, varied routes, reactions, recovery, audio | Good low-cost base |
| Pedestrians | 28 copies of one character circling block sidewalks | Archetypes, destinations, idles, crossings, conversations, reactions | Looks simulated, not inhabited |
| Interactions | Sedan entry/exit | Frequent contextual activities and useful destinations | Largest gameplay gap |
| World reaction | Traffic and pedestrians stop for obstacles | Fleeing, honking, collisions, incidents, escalating responses | Very limited |
| Presentation | Clean low-poly city, procedural trees and clouds | Strong district palette, signs, decals, lighting, sound, animation | Coherent but generic |
| Performance discipline | Shared materials, baked meshes, fixed simulation counts | Streaming, LOD, pooling, budgets | Strong prototype foundation |

## Main Findings

### 1. The Map Is Uniform, Not Too Small

Do not enlarge the map yet. The problem is that all 16 blocks have roughly the
same gameplay value and the five straight roads have equal visual importance.
A compact open world works when each short trip crosses recognizable places.

Recompose the existing footprint into four micro-districts:

- Downtown core: towers, plaza, offices, construction landmark.
- Market strip: low-rise storefronts, awnings, signs, cafe activity.
- Waterfront/industrial edge: warehouses, loading yard, river access, garage.
- Residential/park edge: lower density, trees, basketball or social space.

Keep the traffic-compatible grid initially, but give one road a boulevard
identity and add two non-traffic shortcuts: an alley and a pedestrian passage.

### 2. The First Minute Has No Directed Experience

The player currently starts outside the garage near its side/back, while the
car is hidden inside. There is no immediate destination after entering it.

Change the opening:

1. Spawn on the garage forecourt with the open bay and sedan visible.
2. Frame the construction tower or downtown landmark beyond the car.
3. Present one objective marker at the construction site.
4. Let the player enter the sedan, drive through three checkpoints, interact
   with a signal cabinet, and return.

This one mission validates walking, prompts, driving, traffic, signals,
pedestrians, navigation, mission state, and completion feedback.

### 3. Ambient Systems Need Purposeful Variation

Traffic already has a good lightweight base. Improve perception before count:

- Assign density by road: busy boulevard, normal cross streets, quiet edges.
- Replace fixed rectangular repetition with intersection decisions when the
  route graph is ready: straight, left, or right with seeded probabilities.
- Add brake lights, engine loops, horns, and collision reactions.
- Remove the police-liveried model from ordinary traffic until police behavior
  exists; otherwise it promises a system the game does not have.
- Preserve the clear garage merge area and player-yield behavior.

Pedestrians should become fewer but richer if necessary:

- Add three to five visual archetypes using material/accessory variation.
- Give them destinations: shop door, bench, construction gate, cafe, crossing.
- Add short idle, look-around, sit, talk, and phone states.
- Add signal-aware crosswalk traversal for a subset.
- Add a simple flinch/flee response to a nearby fast vehicle.

Implementation status, 2026-07-11: short pause, look-around, and step-aside
states; signal-aware block-to-block crossings; curb vehicle checks; crossing
hurry behavior; wider NPC-traffic braking margins; and off-screen fixed-pool
recycling are implemented. Destination activities, seated/talking/phone clips,
visual archetypes, and a full flee path remain future work.

Open-world AI guidance emphasizes ambient systemic behavior plus structured
activities, rather than static characters alone. Population simulation must
also remain scheduled and level-of-detail aware.

### 4. Game Feel Is Missing Mostly Because Audio And Feedback Are Missing

There are currently no project-owned gameplay audio systems. Before adding
combat or more map area, add:

- Footsteps tied to movement pace.
- Engine idle/rev loop with pitch from speed.
- Brake/skid cue, collision thud, and horn.
- Quiet city bed, wind, distant traffic, and construction ambience.
- Camera FOV/lag response for sprinting and driving.
- Idle/walk/run blend plus jump and vehicle entry/exit transitions.
- Brake/reverse lights and a restrained speedometer while driving.

Audio and feedback will improve perceived quality more than another district
of static buildings.

### 5. The Streets Need Editing, Not More Props

The current prop pass is dense but repetitive. In particular, 384 bollards and
large tree canopies create clutter without adding destinations.

- Remove duplicated corner bollards and reduce the count by about half.
- Keep intersection sight triangles clear of trees and parked vehicles.
- Replace some blank concrete with loading bays, parking spaces, alleys,
  dumpsters, kiosks, murals, and storefront signs.
- Add facade silhouette variants: balconies, awnings, fire escapes, rooftop
  signs, antennas, water tanks, and parapets.
- Make city boundaries intentional: waterfront, fence/rail edge, park/hill,
  and one visibly closed bridge or road suggesting a larger world.

## Recommended Order

### P0: Prove The Game Loop

1. Rework the garage spawn and make the car immediately visible.
2. Add one five-minute signal-repair driving mission.
3. Add objective marker, completion state, and restart/reset.
4. Add engine, footsteps, collision, and ambient audio.
5. Improve driving camera and basic vehicle feedback.

### P1: Make The City Memorable

1. Establish four micro-district art/prop rules within the same 16 blocks.
2. Add four landmarks and meaningful signs.
3. Reduce repetitive bollards and improve blank lots.
4. Add one alley shortcut and one pedestrian passage.
5. Improve the garage facade, open bay, sign, and forecourt.

### P2: Make The City React

1. Add pedestrian destinations, idles, crossing, and flee behavior.
2. Add road-specific traffic density and randomized intersection choices.
3. Add traffic horns, brake lights, and stuck recovery feedback.
4. Add five useful interactions tied to future vibecoding concepts.

## Explicitly Defer

- Larger map
- Combat and weapons
- Wanted/police system
- Detailed interiors
- Full day/night and weather simulation
- Multiplayer
- More than 24 traffic vehicles or 28 pedestrians

These systems should wait until the five-minute mission is enjoyable and a
standalone macOS build has been profiled against the memory target.

## Reference Direction

- Take-Two's public navigation patent describes road-link density, player-aware
  spawning, vehicle spacing, and ambient-traffic route information:
  https://patents.google.com/patent/US20210316220A1/en
- GDC's "Free-Range AI" session frames open-world characters as ambient
  systemic opportunities that must coexist with structured narrative:
  https://gdcvault.com/play/1020573/Free-Range-AI-Creating-Compelling
- GDC's "Cities at Scale" session emphasizes scheduling traffic flow,
  spawning, LOD, path following, and player interaction under a fixed budget:
  https://gdcvault.com/play/1027985/AI-Summit-Cities-at-Scale
