# LibGGPK2
Library for Content.ggpk of PathOfExile

Rewrite of https://github.com/aianlinb/libggpk

# What's New?
- No longer read all Records at the beginning.
- The new file added will replace existing FreeRecord instead of being appended to the end of the GGPK.
- Correctly handle all NextFreeRecordOffset of FreeRecord.
- No longer allow other programs to access GGPK file when opening it.
- Remove unnecessary code.

# New features in the future
- Merge VisualBundle into VisualGGPK2.
- Implement viewer of .dat, .dds, .ogg etc..
