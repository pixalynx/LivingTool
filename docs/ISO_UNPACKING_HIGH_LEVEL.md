# Guardians Crusade ISO Unpacking (High-Level)

This document explains how the current `FileExtractionService`-driven pipeline finds and extracts hidden data sectors from the Guardians Crusade PS1 image.

## What this unpacker is doing

At a high level, the tool:

1. Reads a hidden sector index (`locsectors.bin`).
2. Converts each index entry into a sector address + length.
3. Pulls each file payload out of the main game image (`gc.bin`) by sector.
4. Groups extracted files into logical folders and assigns game-style names.

The hidden data is not discovered by walking a visible filesystem tree; it is reconstructed from sector locations stored in the executable.

## End-to-end flow

1. `run` command is executed from `LivingTool.Console`.
2. If `locsectors.bin` does not exist, it is extracted from `SLUS_008.11`.
3. `locsectors.bin` is parsed as 8-byte records.
4. Each record yields `(SectorNumber, SizeInSectors)`.
5. Each unique record is extracted from `gc.bin`.
6. The extracted data is written into per-range folders (`LT`, `BF`, `KN`, etc.).
7. Temporary numeric filenames are renamed to target game naming formats.
8. Sector gaps are reported for visibility/debugging.

## Inputs and defaults

- Main game data image: `gc.bin`
- Executable (for hidden index extraction): `SLUS_008.11`
- Hidden sector table output: `locsectors.bin`
- Output directory: `output`

CLI example:

```bash
dotnet run --project src/LivingTool.Console -- run \
  --file gc.bin \
  --output-directory output \
  --loc-sectors-file locsectors.bin \
  --executable SLUS_008.11
```

## Step 1: Locate and extract the hidden sector table

If `locsectors.bin` is missing, the tool extracts a byte range from the executable:

- Start offset: `0x41B04`
- End offset: `0x42A6B` (used as exclusive end in current code)

That region is written to disk as `locsectors.bin` and treated as the lookup table for hidden file extents.

## Step 2: Parse `locsectors.bin` entries

`ReadLocSectors` reads the file in 8-byte chunks:

- Byte 0: minute (BCD)
- Byte 1: second (BCD)
- Byte 2: frame/sector (BCD)
- Byte 3: unused/reserved (currently ignored)
- Bytes 4-7: `sizeInSectors` (`Int32`, little-endian)

Sector conversion:

```text
sectorNumber = (minute * 60 + second) * 75 + frame - 150
```

Notes:

- BCD decoding converts each nibble to decimal.
- `-150` removes PS1 CD lead-in timing offset.
- Parsing stops when fewer than 8 bytes remain.

## Step 3: Extract file payloads from the BIN by sector

For each `(sectorNumber, sizeInSectors)` entry:

1. Compute byte offset:
   `startOffset = sectorNumber * 2352` (`0x930` bytes/sector).
2. Seek to `startOffset` in `gc.bin`.
3. Iterate `sizeInSectors` times and read payload according to sector type.

Two extraction modes exist:

- Standard data sectors (`sectorNumber < 88391`, before folder `M`):
  - Skip 24-byte header.
  - Read 2048-byte payload.
  - Skip 280-byte footer/ECC.
- STR/video region (`sectorNumber >= 88391`):
  - Skip 16-byte STR header.
  - Read 2336-byte STR payload.

Duplicate entries are skipped using a `(sectorNumber, sizeInSectors)` hash set.

## Step 4: Map extracted blobs to folder buckets

The tool uses static sector start points and assigns each file to the range its start sector falls into:

- `LT` starts at `179`
- `BF` starts at `1849`
- `KN` starts at `4261`
- `PB` starts at `4641`
- `ENE` starts at `5650`
- `SYS` starts at `14784`
- `B` starts at `15729`
- `FE` starts at `16030`
- `BGM` starts at `16952`
- `F` starts at `20010`
- `Npc` starts at `85511`
- `M` starts at `88391`
- `M2` starts at `164074`

Extraction writes temporary files as `001.bin`, `002.bin`, etc., then renames by folder-specific patterns like:

- `TOY{0:00}.BIN`, `BF{0:00}.BIN`, `ENEMY{0:000}.BIN`
- `VIDEOS{0:00}.STR`, `ATTRACT.STR`, etc.

## Step 5: Validation/reporting behavior

After extraction, the tool:

- Renames files inside each folder.
- Reports gaps between extracted sector spans.
- Prints total number of unique files extracted.

Gap output includes both:

- LBA range (sector numbers), and
- converted `MM:SS:FF` timing values.

## Practical assumptions of the current implementation

- The source game image is expected to be a raw-sector BIN (`2352` bytes/sector), not an already-scrubbed `2048`-byte ISO dump.
- Hidden extents are driven by the executable-derived LOC sector table, not by visible filesystem entries alone.
- Folder/range mapping is currently hard-coded for Guardians Crusade.
- Repacking is not implemented yet (`Repack()` throws `NotImplementedException`).

## Minimal pseudocode

```text
if locsectors.bin missing:
  locsectors = executable[0x41B04:0x42A6B]
  write(locsectors.bin)

entries = parse_8_byte_records(locsectors.bin)
for each unique (sector, length) in entries:
  data = extract_from_gc_bin(sector, length, raw_sector_rules)
  folder = map_sector_to_folder_range(sector)
  write_temp_file(folder, next_index, data)

rename_all_temp_files_by_folder_patterns()
report_sector_gaps()
```

