# RouteIQ — PR03 Implementation Plan

**Document ID:** VEST-HLP-ROUTEIQ-PLN-PR03  
**Version:** PR03  
**Status:** Accepted. Documents only. Implementation not started.  
**Date:** 4 October 2026  
**Binding:** `PR03 -- Requirements.md`.

There is no step 6. There is no design paper.

---

## 0. Done

Step 1 closed 4 October 2026. Documents Accepted.

Not done: icon, trailing tab, Help view, topic documents.

---

## 1. Order

| Step | Where | Exit |
|---|---|---|
| 1 | This folder | Done. Documents Accepted. |
| 2 | `src/Vestigium.Suite.Network.RouteIQ/Assets/RouteIQ.ico` | 16, 32, 48, 256. Node, one arrow in, two out. `ApplicationIcon` set. Taskbar is not the default mark. |
| 3 | `MainWindow.xaml` | Second strip, docked right, `RadioButton.HorizontalTab`, group name is not `RouteIqMainNav`. Label is Help. Work tabs unchanged. |
| 4 | `Views/HelpView` | Two columns. `RadioButton.VerticalNav` bound to the ten topics. `FlowDocumentScrollViewer` swaps the document. Overview selected on open. Leaving Help restores the prior work tab. |
| 5 | Topic resources | One FlowDocument per topic, order in Requirements §4. Theme brushes only. About reads assembly version and the csproj references. License is the repo MIT text. |

Steps 2–5 are `Vestigium.Suite.Network`. Nothing in this repo compiles.

---

## 2. Watch

| Watch | Failure |
|---|---|
| Icon | A PNG committed as the icon. Or the table mark, which fails at 16px. |
| Strip | Help appended to `NavItems`. That puts it left, in the work group. |
| Selection | Help and Routes sharing a group name. Checking Help clears the work tab. |
| Theme | A FlowDocument with `Foreground="Black"`. Dark packs fail. |
| About | Hand-typed versions. They rot the day a package moves. |
| License | A rewritten MIT paragraph. The repo file is the text. |
| Scope | Markdig, `DocumentViewer`, an About dialog beside the About topic, a Helpers.RouteIQ package. |

---

## 3. Out of this plan

Write-route form. Settings moved to the right rail. A Logs topic. A markdown pipeline. Closing Suite.Network PR01. Publishing anything.

---

## 4. Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 4 Oct 2026 | Plan written. Step 1 Accepted. Steps 2–5 not started. |
