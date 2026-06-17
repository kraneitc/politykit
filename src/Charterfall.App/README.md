# Charterfall App

This is the Milestone 1 lightweight prototype shell for Charterfall. It is the game-layer app surface for campaign session state, player-facing flow, placeholders, and presentation. Deterministic simulation runs remain owned by PolityKit.

## Launch

```powershell
dotnet run --project src\PolityKit.Sim.Api
dotnet run --project src\Charterfall.App
```

Open the printed Charterfall local URL. The app opens into the Draft state and exposes Draft, Inquiry, Amendment, Comparison, and Final Outcome through visible navigation and action buttons.

## Verification

```powershell
dotnet build PolityKit.slnx
dotnet test PolityKit.slnx --no-build
```

The Draft Resolve action calls the PolityKit API at `PolityKitApi:BaseUrl`, which defaults to `http://localhost:5020` for local development.

## Scenario Selection

The Draft view shows the fixed Milestone 1 Greywater Compact crisis order:

1. Failed Harvest
2. Fever Season
3. Supply Office Scandal

Chapter 1 is unlocked by default. Future chapters are visible as campaign cards but locked until campaign progression is wired in a later slice.

## Charter Clause Selection

The Draft view lets the player choose one simulation-active allocation method and a small set of preset-backed or presentation-only clauses. The run input preview shows the future PolityKit model IDs and parameters separately from Charterfall-only clause IDs.

## Run Creation

The Draft view submits only executable scenario data and authoritative mapped clauses to `POST /api/runs`. Charterfall-only fields such as selected clause IDs, game-layer-only clauses, chapter state, and campaign notes remain in app session state and are not sent to PolityKit.
