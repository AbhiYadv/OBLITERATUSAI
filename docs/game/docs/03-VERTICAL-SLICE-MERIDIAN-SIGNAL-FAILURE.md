# Vertical Slice — Meridian Signal Failure

## Purpose

This mission proves that Nexo World is more than a city demo. It combines movement, pedestrians, traffic lights, traffic, vehicle traversal, buildings, evidence, AI agents, decision-making, validation, and visible world changes in one 20–35 minute experience.

## Pitch

A cascading connection failure destabilizes Meridian District. Traffic signals flicker, service kiosks fail, citizens gather near blocked crossings, and the Operations Center receives incomplete alerts. The player must collect evidence, coordinate with specialists, drive to the Infrastructure Annex, choose a safe intervention, and validate recovery.

## Locations

Operations Center, Meridian Plaza, Telemetry Kiosk, Tool Row, Parking Garage, Infrastructure Annex, Signal Junction, AAR Review Room.

## Main cast

- **Maya Rao — Operations Dispatcher:** gives mission and warns against acting without evidence.
- **Elias Park — Platform Engineer:** knows about the recent deployment.
- **Nia Brooks — DBRE Specialist:** explains connection saturation and requires evidence.

## Mission flow

1. Normal district.
2. Incident trigger: signals blink, crossing stops, kiosks fail, pedestrians wait.
3. Meet Maya and receive symptom evidence.
4. Inspect telemetry: connection saturation, error spike, timing.
5. Ask Elias: deployment and config evidence.
6. Take the sedan or walk to the Infrastructure Annex.
7. Review evidence with Nia.
8. Choose intervention:
   - restart app tier: temporary relief
   - increase pool: unsafe worsening
   - rollback timeout config and drain stale connections: correct
9. Validate service, signals, kiosk, and district state.
10. Recovery: traffic resumes, pedestrians cross, signs relight, gate opens.
11. AAR explains evidence, safety, decision, validation, and disruption.

## Acceptance criteria

- completable from fresh load
- best intervention requires evidence
- wrong interventions are recoverable
- world visibly changes
- vehicle useful but not mandatory
- understandable without jargon overload
- 20–35 minute first completion
