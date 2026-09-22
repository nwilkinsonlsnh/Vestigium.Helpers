# Vestigium.Documentation

Suite document store. Project-root `README.md` files stay with the library and link here by **folder**, not by filename.

## Area folders

Each library under `Vestigium/Helpers/{Name}/` uses the same numbered areas:

| Area | Role |
|---|---|
| `000 -- Archived` | Superseded revisions |
| `001 -- Implementation Plan` | Current plan, or a status README if none |
| `002 -- Requirements Document` | Binding contract |
| `003 -- Design Document` | Architecture |
| `004 -- Developers Guide` | How a host uses the library |

## Rule that keeps GitHub links alive

Git does not store empty directories. A Visual Studio `<Folder Include="..." />` item only exists in Solution Explorer.

**Every numbered area folder must contain at least one tracked file.**

- Current document when one exists (`Requirements_v2.0.md`, …).
- Otherwise a short `README.md` that states **Status: none** and points at `000 -- Archived` if history exists.

Do not rely on `.gitkeep` unless the folder must stay silent. Prefer `README.md` so the folder explains itself.

When a document moves to archive, leave a `README.md` behind in the live folder so the GitHub URL on the project README does not 404.
