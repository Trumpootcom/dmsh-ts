# Porting roadmap

- [x] Port the default rectangular grid, sheet badges, and optional coordinate labels on map sheets.
- [ ] Match the TypeScript overview layout, including its grid and sheet markings.
- [ ] Compare PDFs made with the same `map.png` and `wmsh_ts/src/default.json`. Check page count, paper size and orientation, sheet order, map placement, grid spacing, overlap, coordinate labels, and the overview page.
- [ ] Verify that batch mode continues after invalid images, filename conflicts, and archive failures, and that each failed source remains in the incoming folder.
- [ ] Port the remaining grid styles and options, including isometric and hex grids and adaptive contrast.
- [ ] Review the remaining image preprocessing differences, including the TypeScript JPEG decoder memory limit.
- [ ] Verify self-contained CLI packages on Windows and macOS.
