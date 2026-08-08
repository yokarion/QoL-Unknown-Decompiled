# QoL-Unknown-Decompiled

Decompiled source mirror of **QoL Unknown**, the quality of life and Discord
Rich Presence mod for Casualties: Unknown, published at
[jimmyking9999999/QoL-Unknown](https://github.com/jimmyking9999999/QoL-Unknown).

Everything under [decompiled/](decompiled/) is generated automatically from the released DLL
by [dotnet-autodecompiler](https://github.com/Yokarion/dotnet-autodecompiler)
and is overwritten on every update. Do not edit it by hand and do not open pull
requests against it.

This exists so the mod's code can be read and diffed between releases. It is a
reconstruction produced by a decompiler, not the author's original source, so
local variable names and compiler-generated members will not match what the
author wrote.

## What is mirrored

The mod ships as a single assembly, `QoL Unknown.dll`, attached to each GitHub
release. It decompiles into `decompiled/QoL Unknown/`.

Embedded resources are not extracted. This build carries roughly 4 MB of them -
UI images, an asset bundle, and its dependencies packed in by Costura
(BepInEx, HarmonyX, Mono.Cecil, DiscordRPC and friends) - none of which is
readable source. Skipping them keeps the mirror at about a quarter of the size
with nothing of interest lost. Those packed dependencies are libraries by other
authors and are not part of this mod.

`decompiled/README.md` lists what was decompiled, the third-party dependencies
it references, and anything that could not be decompiled.
`decompiled/.autodecompiler.json` records which upstream release each mirror was
produced from, including the SHA-256 of the DLL.

## How it updates

`.github/workflows/decompile.yml` polls the upstream repository weekly and calls
the reusable workflow in
[dotnet-autodecompiler](https://github.com/Yokarion/dotnet-autodecompiler).
When the released DLL is unchanged the run stops without a commit, so the
history contains one commit per actual upstream release.

To mirror a specific older release, run the workflow manually from the Actions
tab and enter its tag.
