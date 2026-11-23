# Porting roadmap

- [x] Port the default rectangular grid, sheet badges, and optional coordinate labels on map sheets.
- [x] Port the default TypeScript overview layout, including its legend, grid, coordinate bars, and sheet badges.
- [x] Compare the default exports for page count, paper size and orientation, sheet order, map placement, overlap, and text positions, sizes, and weights.
- [ ] Visually compare grid spacing, stroke appearance, and labels across sheet seams and on the overview.
- [x] Port rectangular grid dash patterns from `grid.ts`, including `dash-1` through `dash-16` where defined, `corners`, and legacy patterns and aliases.
- [ ] Compare dashed exports against TypeScript on individual sheets and the overview, including dash phase across overlaps.
- [x] Port rotated rectangular and isometric grids using the TypeScript lattice math.
- [ ] Compare rotated and isometric exports with TypeScript, including offsets and dash phase across sheet overlaps.
- [ ] Port hex grids, difference blend, and adaptive contrast. Compare their output with the TypeScript reference.
- [ ] Check template field validation against TypeScript and report unsupported options clearly when the renderer cannot honor them.
- [ ] Verify that batch mode continues after invalid images, filename conflicts, and archive failures, and that each failed source remains in the incoming folder.
- [ ] Review the remaining image preprocessing differences, including the TypeScript JPEG decoder memory limit.
- [ ] Verify self-contained CLI packages on Windows and macOS.
