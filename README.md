# DreadPKG
## Metroid Dread PKG unpack\repack tool

This tool allows you to unpack and repack Metroid Dread PKG files.

Features:
- Can Unpack and Repack all Metroid Dread PKG files (Not including snd)
- File sizes do not have to stay the same for repacking, allowing more modification to files.

**There is no error checking, if you experience crashes. Run the tool from a command line to read output of the error
Most common errors are caused by not having permission to create files\directories.
Other errors can be caused by having .bak of modified files (usually created by hex editors) or any other additional files in the unpacked directory**
*Error checking and handling will be added later.

How to use from Source:
- Download the source code
- Open the project in VS and build.
- Move the compiled tool to the root of romfs (or create a packs directory in the tools location and put the pkg files you want to work with there)
- Run the tool
- Choose Unpack or Repack, once finished you should see an unpacked or repacked directory with your files
