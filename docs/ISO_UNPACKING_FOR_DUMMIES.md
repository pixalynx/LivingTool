# Guardians Crusade Unpacking (For Dummies)

This is the simple version of what the tool is doing.

## Imagine a giant storage box

Think of the game file (`gc.bin`) like one huge storage box made of tiny slots called **sectors**.

- Each sector is a fixed-size chunk of data.
- The game hides useful files in specific sector ranges.
- Those hidden files are not easy to see by just browsing normal folders.

## So how does the tool find hidden files?

It uses a **map**.

That map is called `locsectors.bin`.

- If `locsectors.bin` already exists, the tool uses it.
- If it does not exist, the tool pulls it out of the game executable (`SLUS_008.11`) from a known byte range.

So the executable contains the directions to the hidden data.

## What is inside that map?

The map is a list of entries.  
Each entry says:

1. Where to start reading (sector number)
2. How many sectors to read (size)

That is enough information to reconstruct one hidden file.

## How unpacking actually happens

For each entry in the map, the tool does this:

1. Go to the start sector in `gc.bin`
2. Read the requested number of sectors
3. Keep only the real payload bytes (skip sector metadata/header parts)
4. Save the result as a file

It repeats this for every map entry.

## Why are there different extraction rules?

Some data is normal game data, and some is video (`.STR`) data.

- Normal sectors use one byte layout.
- Video sectors use a different layout.

The tool switches rule sets based on where the sector is in the disc.

## Where do extracted files go?

The tool sorts files into folders like `LT`, `BF`, `ENE`, `Npc`, `M`, etc.

These folders are based on predefined sector ranges (known from reverse engineering).

After extraction, files are renamed to game-style names such as:

- `TOY01.BIN`
- `ENEMY001.BIN`
- `VIDEOS01.STR`

## In one sentence

The unpacker uses a hidden location map from the executable to pull specific sector ranges out of the game BIN, then saves them as usable files.

If you want NPC file internals (models, script records, name/dialog pointers), see `NPC_BIN_UNPACKING.md`.
