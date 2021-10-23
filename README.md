# LibGGPK2
Library for Content.ggpk of game PathOfExile.

Rewrite of https://github.com/aianlinb/libggpk
## VisualGGPK2
A visual program to view/edit ggpk file.

![image](https://github.com/aianlinb/LibGGPK2/blob/master/.github/example.png)
## What's New?
- Directly access the files in bundles.
- No longer read all Records of GGPK at the beginning.
- The new file added will replace existing FreeRecord instead of being appended to the end of the GGPK.
- Correctly handle all NextFreeRecordOffset of FreeRecord.
- No longer allow other programs to modify GGPK file when opening it.
- Left click the folder to expand it.
- Replacing by directory.
- Filter files by their path.
- Recovering files from patch server.
- Vista style folder selector.
- Export/Replace in background.
- ProgressBar to view the current work progress.
- Fix DDS viewer.
- Directly edit and save in TextViewer.
- Custom exception window instead of crashing.
- Port from .NET Framework to .NET Core.
- Remove unnecessary code.
- Add support to .dat64 .datl .datl64 files.
- Allow editing the whole dat file.
- Import data from csv to a dat file.
- Automatically check for updates.
- Batch convert dds files to png.
- Allow zooming with the mouse wheel in ImageViewer