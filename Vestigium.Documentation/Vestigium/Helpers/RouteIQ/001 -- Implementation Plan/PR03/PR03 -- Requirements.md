# RouteIQ — PR03 Requirements

**Document ID:** VEST-HLP-ROUTEIQ-SRS-PR03  
**Version:** PR03  
**Status:** Accepted. Documents only. Implementation not started.  
**Date:** 4 October 2026  
**Host:** `Vestigium.Suite.Network.RouteIQ`  
**APPID:** `RouteIQ`  
**Binding:** This file wins for the icon, the Help tab, and the topic rail. It does not win on protocol. Helpers.Network still wins on routes, neighbors, and the default-route deny. Helpers.Kql still wins on the query language.

If implementation and this file disagree, this file wins.

This folder is not a package. Do not create `Vestigium.Helpers.RouteIQ`.

---

## 0. Purpose

The window needs a mark that reads as this host at 16px, a Help tab that explains the window that exists, and two trailing topics for About and License.

Dissent, recorded: About was rejected as a topic and proposed as a dialog. The owner put About on the rail, and License beside it. The dialog is out for this slice. If the page feels wrong, reopen. Do not ship both a page and a dialog.

---

## 1. Decisions

| # | Decision | Lock |
|---|---|---|
| 1 | Icon is a node, one arrow in, two arrows out | Not a map pin, compass, truck, globe, NIC, or ping wave. |
| 2 | Icon is an `.ico` | 16, 32, 48, 256. `Assets\RouteIQ.ico`. `ApplicationIcon` on the csproj, same as PingIQ, DnsIQ, NicIQ. A PNG concept is not the icon. |
| 3 | Help is a trailing tab | Far right. Own radio group. It does not join `RouteIqMainNav`. Work tabs stay left. Settings stays with them. |
| 4 | Leaving Help restores the work tab | Opening Help does not reset the strip to Routes. |
| 5 | Rail is `RadioButton.VerticalNav` | One group. Overview selected when Help opens. Not a button list. A button does not hold selection. |
| 6 | Page is a `FlowDocumentScrollViewer` | One FlowDocument resource per topic. Theme brushes. No hard-coded black. |
| 7 | No markdown renderer | Markdig is out. Seven-plus short topics do not earn a parser. |
| 8 | `DocumentViewer` is the wrong control | It is the XPS viewer. Help is not a fixed-page report. |
| 9 | About is a topic | Name, assembly version, one sentence, copyright line, own packages, third party. Not a dialog this slice. |
| 10 | License is a topic | MIT text from the repo `LICENSE`. Copyright (c) 2026 Vestigium / Wilkinson Business. Do not rewrite it. |
| 11 | Rail does not scrape headings | The button list is the list. If a page and its button disagree, the button is wrong. |
| 12 | No new package | Code lands in `src/Vestigium.Suite.Network.RouteIQ`. |

---

## 2. Icon

Dark blue tile. White node. One arrow entering from the left. Two arrows leaving to the right, forked. Must still read at 16px in the taskbar and on the window.

Rejected marks: map pin, compass, truck, globe, table-of-rows. The table mark turns to mush at 16px.

---

## 3. Help chrome

The current strip is one left-aligned `ItemsControl` bound to `NavItems`. Far right is a second group, docked right, `RadioButton.HorizontalTab`, own group name.

| Piece | Rule |
|---|---|
| Tab label | Help |
| Body | Two columns. Rail left, document right. |
| Rail style | `RadioButton.VerticalNav` |
| Document | `FlowDocumentScrollViewer` |
| Code sample, if any | `RichTextBox.CodeViewer` for that block only |
| About button | None. About is a topic. |

---

## 4. Topic rail

Order is the order. No section headers.

| Topic | Page |
|---|---|
| Overview | Print the stack, ask one neighbor, do not write the default route. Name the six work tabs as they appear on the strip. Log path: `%ProgramData%\Vestigium\Logs\RouteIQ\`. |
| Routes | Family All / IPv4 / IPv6, refresh, the columns on that grid. |
| Neighbors | Cache print, and probe. One address. Blank does not start. |
| Connections | Snapshot, then watch added, dropped, and returned rows. |
| NetBIOS | Local names and the remote name cache. |
| LMHOSTS | Read the system file. Not an editor. |
| Query | KQL filters the print in front of you. Not a second product. |
| Will not do | No add, change, or remove. No `0.0.0.0/0` or `::/0` write. No `route.exe`, `netsh`, or `arp`. |
| About | Assembly name, version, the one-sentence job, copyright line, own packages, third party. |
| License | The MIT notice. Full text. One copy. |

Settings is not a topic. It is a work tab. Logs are two lines on Overview, not a page.

About lists, and does not invent:

| Kind | Names |
|---|---|
| Own | Themes, Controls, StatusBar, NumericUpDown, Converters, QueryBar, Helpers.Kql, Shell. Versions from the references, not from memory. |
| Third party | `Microsoft.Extensions.DependencyInjection`. MIT. One line. |

Do not credit Vestigium packages as third party. Do not paste the MIT text six times on About. License holds the notice.

---

## 5. What this slice does not do

- Create `Vestigium.Helpers.RouteIQ`
- Add Markdig, or any markdown control
- Use `DocumentViewer`
- Ship an About dialog beside the About topic
- Move Settings to the right rail
- Document a write-route form
- Close Suite.Network PR01
- Change Helpers.Network or Helpers.Kql

---

## 6. Accept

1. Window and taskbar show the node icon at 16px. It is not the default WPF mark.
2. Help sits at the far right and does not steal selection from the work tabs.
3. Opening Help shows Overview. Closing it returns to the work tab that was selected.
4. The rail has the ten topics in §4, in that order, and only those.
5. Dark theme does not leave the document black-on-black.
6. About shows the assembly version and does not hard-code a package version that the csproj does not reference.
7. License text matches the repo `LICENSE`, including the copyright line.
8. No new package, no Markdig reference, no `DocumentViewer`.

---

## 7. Document control

| Version | Date | Change |
|---|---|---|
| PR03 | 4 Oct 2026 | Icon, Help tab, ten-topic rail. About and License are topics. Dialog rejected after owner decision. |
