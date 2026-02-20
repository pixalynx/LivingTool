# Guardians Crusade NPC BIN Unpacking (Current Findings)

This document describes the NPC BIN parsing currently implemented in LivingTool.

Scope:

- Parser: `src/LivingTool.Core/Features/GameStructure/Npc/NpcFileData.cs`
- Tests: `tests/LivingTool.Core.Tests/Features/GameStructure/Npc/NpcFileDataTests.cs`
- Sample files:
  - `tests/LivingTool.Core.Tests/Features/GameStructure/Npc/TestData/NPC07.BIN`
  - `tests/LivingTool.Core.Tests/Features/GameStructure/Npc/TestData/NPC08.BIN`
  - `tests/LivingTool.Core.Tests/Features/GameStructure/Npc/TestData/NPC11.BIN`

## Top-level file layout

`NPCxx.BIN` starts with a pointer table.

1. `u32 headerSizeBytes` at offset `0x00`
2. `headerSizeBytes / 4` top-level pointers (absolute file offsets)
3. Pointer `0` targets metadata section ("entry0")
4. Pointer `1` targets entity/text section ("entry1")
5. Remaining pointers target model/other chunks

Examples:

- `NPC07.BIN`: `headerSize = 0x60`, pointer count `24`
- `NPC08.BIN`: `headerSize = 0x70`, pointer count `28`
- `NPC11.BIN`: `headerSize = 0x78`, pointer count `30`

## Entry0 (metadata + pointer mini-table)

At top-level `entry0` offset:

- `u16 groupACount`
- `u16 groupBCount`
- Then a list of `u32` relative offsets terminated by `0x00000000`

Current parser behavior:

- Exposes counts as `GroupACount` and `GroupBCount`
- Converts relative offsets to absolute pointers and stores them in `EntriesB`
- Validated rule in samples:
  - `groupACount + groupBCount + 2 == top-level pointer count`

Sample values:

- `NPC07.BIN`: `groupA=12`, `groupB=10`
- `NPC08.BIN`: `groupA=16`, `groupB=10`
- `NPC11.BIN`: `groupA=19`, `groupB=9`

## Entry1 (entity/script/text section)

At top-level `entry1` offset:

1. `u32 entityRecordCount`
2. Followed by `entityRecordCount` fixed records of 16 bytes each

Current record interpretation:

```c
struct NpcEntityRecord {
  int  ScriptAOffset; // relative to entry1 start
  uint PackedValue;   // low16 = TypeId, hi bytes = small signed params
  int  ScriptCOffset; // relative to entry1 start
  uint Flags;         // commonly 0x0002, with variants
}
```

Derived fields:

- `TypeId = PackedValue & 0xFFFF`
- `ParameterA = (sbyte)((PackedValue >> 16) & 0xFF)`
- `ParameterB = (sbyte)((PackedValue >> 24) & 0xFF)`

Sample counts:

- `NPC07.BIN`: `59` records
- `NPC08.BIN`: `102` records
- `NPC11.BIN`: `54` records

## Names and dialogue extraction

Text extraction is two-stage:

1. Pointer-driven extraction from script bytes.
2. Fallback text-bank extraction for lines not referenced directly by opcodes.

Current pointer-driven logic:

1. For each entity record, scan script bytes in `ScriptAOffset` region.
2. Name pointer:
   - Find first opcode `0x1F`, then read the following little-endian `u16` offset.
3. Dialogue pointers:
   - Find opcode `0x01`, then read the following little-endian `u16` offset.
4. Decode text at those offsets until null terminator.
5. Normalize control bytes:
   - `0x06` and `0x07` are converted to newline separators.

Fallback logic:

1. Enumerate all null-terminated normalized strings in entry1.
2. Start from the earliest extracted text offset.
3. Derive names as the first contiguous block of short name-like strings.
4. Treat remaining strings as dialogue candidates.

Safety/heuristics in current parser:

- Scan range is from current script start to next script start.
- Pointers outside section bounds are ignored.
- Pointers must reference a valid string start boundary (`previous byte == 0x00`).
- Name and dialogue offsets are deduplicated.
- Dialogue strings shorter than 4 chars are filtered out.
- Name candidates are filtered (length/character rules) to avoid false positives.

Known-good samples:

- `NPC07.BIN` includes names like `Algo`, `Woman`, `Mary`.
- `NPC08.BIN` includes names `Mayor`, `O'Neal`, `Blue Cat`.
- `NPC11.BIN` includes names like `Informer`, `Merchant`, `Sad Man`.

## Model signature detection

Current parser scans top-level pointers and classifies by first dword:

- `0x00000041` -> TMD chunk (added to `TmdOffsets`)
- `0x00000010` -> TIM chunk (added to `TimOffsets`)

In current test samples:

- `NPC07.BIN`: `13` TMD sections, `0` TIM sections
- `NPC08.BIN`: `16` TMD sections, `0` TIM sections
- `NPC11.BIN`: `19` TMD sections, `0` TIM sections

## Console decode output

The `npc` command returns structured JSON by default:

```bash
livingtool npc --file output/NPC/NPC08.BIN
```

Optional text mode:

```bash
livingtool npc --file output/NPC/NPC08.BIN --format text
```

JSON fields include:

- metadata (`HeaderSize`, `TopLevelEntries`, `GroupACount`, `GroupBCount`)
- record/text counts (`EntityRecordCount`, `NameCount`, `DialogueCount`)
- offsets (`TmdOffsets`, `TimOffsets`, `NamePointerOffsets`, `DialoguePointerOffsets`)
- decoded content (`Names`, `Dialogues`)

## What is confirmed vs inferred

Confirmed by parser/tests:

- Header sizing and pointer count behavior
- Entry0 count fields and count relationship
- Entry1 fixed record sizing
- Opcode-driven retrieval of names/dialogue pointers
- Fallback text-bank extraction for non-pointered dialogue entries
- TMD magic detection at top-level chunk starts

Still inferred / subject to refinement:

- Exact meaning of `ParameterA`, `ParameterB`, and `Flags`
- Full opcode/script VM semantics
- Whether any NPC files contain real TIM data in this folder/range

## Validation

Run:

```bash
dotnet test tests/LivingTool.Core.Tests/LivingTool.Core.Tests.csproj --filter NpcFileDataTests
```

Current NPC tests assert:

- Header/pointer offsets
- Entry0 group counts
- Entry1 record structure
- TMD/TIM signature counts
- Name pointer and dialogue extraction
- NPC08 regression behavior (`"f"` is not treated as a name; high dialogue coverage)
