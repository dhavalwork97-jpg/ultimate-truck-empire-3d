# Production Trailer Asset Intake

This directory is the controlled intake point for production trailer art.

## Release gate

A trailer must not ship in a commercial build until its manifest entry has:

- `status` set to `verified`
- an explicit commercial-use license type
- a source URL or documented private source
- creator/rights-holder attribution where required
- a checked-in license/permission text file at `licenseTextPath`
- an imported prefab that passes the trailer catalog, prefab contract, and physics validators

`pending` entries are intentional placeholders. They do **not** grant permission to use an asset.

## Accepted rights

The manifest currently permits:

- CC0
- an explicit commercial license
- written custom permission that grants the required commercial/game use

Do not add assets copied from game mods, extracted/decompiled games, or sources whose commercial rights cannot be established.

## Intake layout

Each trailer gets its own folder:

`Assets/TrailerSystem/Imports/Trailers/<trailer-id>/`

Keep source/license documentation alongside the imported art where practical. The import pipeline only processes supported model formats and does not fetch external content.

## Required production replacement

Generated sockets and physics proxies are integration placeholders. Before release, replace them with authored:

- Kingpin
- cargo socket
- wheel sockets
- collision geometry
- LODs
- mobile-appropriate materials/textures

Use the existing editor validators after each production asset is integrated.
