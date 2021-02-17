# Dark Domains

An experimental 4X.

Built following the tutorials written by Catlike Coding, specifically the Hex series: https://catlikecoding.com/unity/tutorials/hex-map

## Beyond the tutorials

Ideas on where to go next:

- a 'bevel' on cliffs and rivers: an extra pair of vertices to create a slight slope before the steep one
- pixel art textures for terrain, and sprites for trees, towns etc (up the density on trees)
- when a cell is underwater, it can have a river on any side, in or out
- expanding lakes - join with any river along side by expanding
    - identify river 'id' for cells, to ensure loop rivers dont exist (river that feeds into itself)
- using a byte in memory to represent all low int values, but signed. sbyte? or int16? (latter is cls compliant, but larger)