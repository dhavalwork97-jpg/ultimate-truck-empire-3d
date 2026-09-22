# Jazzycat v1.3 ATS trailer/cargo source audit

This audit records the technical intake of the user-supplied `trailers_and_cargo_pack_by_Jazzycat_v1.3_ats.7z` archive.

## Commercial disposition

**Reference-only. Not release-cleared.**

The supplied README attributes content to multiple third parties and identifies the package as an ATS game mod. The archive did not establish commercial redistribution rights for this project. Therefore the original SCS/PMD/PMG/PMC/texture/material assets must not be copied into shipping `Assets/` as production art.

This audit intentionally does **not** change any launch trailer license row to `verified`.

## Technical intake

- Approx. 742 files in the extracted base/definition content
- 169 DDS textures
- 250 materials
- 172 TOBJ files
- 59 PMD models
- 59 PMG models
- 32 PMC collision files
- 38 English cargo definitions
- 3 preview images

Observed trailer families include lowboy, Fontaine heavy/flatbed, cement tanker, grain hopper, container chassis, and additional agricultural/grain/food-cistern references.

Observed cargo includes agricultural machinery, excavator/forklift/telehandler/compactor equipment, and large industrial cargo.

## Conversion policy

The pack can inform **technical reference and category mapping**. It cannot be treated as a commercially cleared source merely by changing textures, removing branding, retopologizing, or converting file formats.

For production, use newly authored geometry or an independently verified commercially redistributable source, then pass the existing trailer prefab, physics, mobile-budget, catalog, and launch-set validators.

See the machine-readable inventory in `JazzycatV13ReferenceAudit.json`.
