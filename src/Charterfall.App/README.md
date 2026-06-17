# Charterfall App

This is the Milestone 1 lightweight prototype shell for Charterfall. It is the game-layer app surface for campaign session state, player-facing flow, placeholders, and presentation. Deterministic simulation runs remain owned by PolityKit.

## Launch

```powershell
dotnet run --project src\Charterfall.App
```

Open the printed local URL. The app opens into the Draft state and exposes Draft, Inquiry, Amendment, Comparison, and Final Outcome through visible navigation and action buttons.

## Verification

```powershell
dotnet build PolityKit.slnx
dotnet test PolityKit.slnx --no-build
```

The current shell uses in-memory services and placeholder run records. Placeholder records are marked as integration pending and are not authoritative PolityKit output.
