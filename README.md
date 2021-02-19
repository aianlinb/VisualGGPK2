# LibGGPK2
Library for Content.ggpk of game PathOfExile.

Rewrite of https://github.com/aianlinb/libggpk
## VisualGGPK2
A visual program to view/edit ggpk file.

![image](https://github.com/aianlinb/LibGGPK2/blob/master/.github/example.png)
## What's New?
- Directly access the files in bundles.
- No longer read all Records at the beginning.
- The new file added will replace existing FreeRecord instead of being appended to the end of the GGPK.
- Correctly handle all NextFreeRecordOffset of FreeRecord.
- No longer allow other programs to modify GGPK file when opening it.
- Left click the folder to expand it.
- Replacing by directory.
- Filter files by their path.
- Recovering files from patch server.
- Vista style folder selector.
- Export/Replace in background.
- Fix DDS viewer.
- Directly edit and save in TextViewer.
- Custom exception window instead of crashing.
- Port from .NET Framework to .NET Core.
- Remove unnecessary code.
## Working on . . .
- Viewer of .ogg .bank .bk2 etc..
- .dat Editing