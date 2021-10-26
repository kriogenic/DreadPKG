# DreadPKG
## Metroid Dread PKG unpack\repack tool

This tool allows you to unpack and repack Metroid Dread PKG files.

Features:
- Can Unpack and Repack all Metroid Dread PKG files (Not including snd)
- File sizes do not have to stay the same for repacking, allowing more modification to files.
- Additional files can be added to the PKG - Although with no way to reference them in game as of yet, is useless?

**If you experience any crashes, run the tool from the command line to stop the window closing and report the issue.**

How to use from Source:
- Download the source code
- Open the project in VS and build.
- Move the compiled tool to the root of romfs (or create a packs directory in the tools location and put the pkg files you want to work with there)
- Run the tool
- Choose Unpack or Repack, once finished you should see an unpacked or repacked directory with your files
