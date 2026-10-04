# RouteIQ — PR03 document list

**Host:** `Vestigium.Suite.Network.RouteIQ`  
**Status:** Live — [PR03 -- Implementation Plan](PR03/PR03%20--%20Implementation%20Plan.md)

This file is the queue keeper. It is not the plan. Do not delete it when the plan is idle.

## This slice

| Paper | Wins on |
|---|---|
| [PR03 -- Requirements.md](PR03/PR03%20--%20Requirements.md) | Icon, Help tab, topic rail, About, License |
| [PR03 -- Implementation Plan.md](PR03/PR03%20--%20Implementation%20Plan.md) | Order of work |

No design paper. The requirements name the controls. A design file would restate them.

## Layout

```
001 -- Implementation Plan/
  README.md                              this file
  PR03/
    PR03 -- Requirements.md
    PR03 -- Implementation Plan.md
```

Follow-ons stay in `PR03/`. They do not get a sibling folder.

## Out of this queue

| Item | Where it lives |
|---|---|
| Route print, neighbor probe, default-route deny | Suite.Network `Requirements_v1.0.md` and Helpers.Network |
| KQL language and completion | `Vestigium/Helpers/Kql/` |
| A `Vestigium.Helpers.RouteIQ` package | Does not exist. Do not create it. |
