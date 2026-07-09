# Meridian District — World and Level Design

## Scope

Approximately **400 m × 400 m**. Dense, navigable, purposeful.

## Goals

- one meaningful destination visible from most intersections
- distinct landmark silhouettes
- looped, simple vehicle routes
- walking shortcuts
- mission naturally moves through the district
- no destination more than roughly two minutes away by car

## Zones

- Operations Core
- Meridian Plaza
- Tool Row
- Infrastructure Annex
- Learning Campus
- Mobility Block / Garage
- Project Garden

## Road hierarchy

- perimeter loop road
- central avenue
- secondary service street
- two pedestrian shortcuts
- two marked crossings
- one mission-critical signal junction

## Suggested coordinates

- Operations Center: `(0, 0, 0)`
- Plaza: `(0, 0, -55)`
- Tool Row: `(-90, 0, -75)`
- Garage: `(-135, 0, 40)`
- Infrastructure Annex: `(105, 0, 95)`
- Learning Campus: `(115, 0, -90)`
- Signal Junction: `(45, 0, 20)`
- Project Garden: `(-20, 0, 115)`

Adjust to the current city plan instead of rebuilding the coordinate system.

## Streaming boundary

Use a 4 × 4 logical grid of roughly 100 m cells. A cell defines static geometry, active NPCs, traffic relevance, mission relevance, audio zone, high-detail assets, and fallback assets.

The first slice may load all cells, but the boundary must exist before expansion.
