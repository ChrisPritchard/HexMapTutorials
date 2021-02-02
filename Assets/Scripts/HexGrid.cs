
namespace DarkDomains
{
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    public class HexGrid : MonoBehaviour
    {
        private int chunkCountX = 8, chunkCountZ = 6;

        public int CellCountX => chunkCountX * HexMetrics.ChunkSizeX;
        public int CellCountZ => chunkCountZ * HexMetrics.ChunkSizeZ;

        public Color DefaultColour = Color.white;

        public HexGridChunk ChunkPrefab;
        public HexCell CellPrefab;
        public Text CellLabelPrefab;
        public Texture2D NoiseSource;

        public int Seed;

        HexGridChunk[] chunks;
        HexCell[] cells;

        private void Awake()
        {
            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);

            CreateChunks();
            CreateCells();
        }

        private void OnEnable() 
        {
            if(HexMetrics.NoiseSource)
                return;

            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);
        }

        private void CreateChunks()
        {
            chunks = new HexGridChunk[chunkCountX * chunkCountZ];

            for(var z = 0; z < chunkCountZ; z++)
                for(var x = 0; x < chunkCountX; x++)
                    CreateChunk(x, z);
        }

        private void CreateChunk(int x, int z)
        {
            var chunk = Instantiate<HexGridChunk>(ChunkPrefab);
            chunk.transform.SetParent(this.transform);
            chunk.ShowUI(false);

            var index = z * chunkCountX + x;
            chunks[index] = chunk;
        }

        private void CreateCells()
        {
            cells = new HexCell[CellCountX * CellCountZ];

            for(var z = 0; z < CellCountZ; z++)
                for(var x = 0; x < CellCountX; x++)
                    CreateCell(x, z);
        }

        private void CreateCell(int x, int z)
        {
            var px = (x + z/2f - z/2) * (2 * HexMetrics.InnerRadius);
            var pz = z * (1.5f * HexMetrics.OuterRadius);
            var position = new Vector3(px, 0f, pz);

            var cell = Instantiate<HexCell>(CellPrefab);
            cell.transform.localPosition = position;
            cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

            var index = z * CellCountX + x;
            cells[index] = cell;

            // connect neighbours, working backwards. e.g. connect the prior, and the bottom two corners if available
            // the setneighbour function does the reverse, so connecting back will conneck the prior cell to the current one too
            // in this way, all cells are connected to their neighbours

            if (x != 0)
                cell.SetNeighbour(HexDirection.W, cells[index - 1]);
            if (z != 0)
            {            
                if (z % 2 == 0) // non 'shunted' row, so always has bottom right, but first doesnt have bottom left
                {
                    cell.SetNeighbour(HexDirection.SE, cells[index - CellCountX]);
                    if (x != 0)
                        cell.SetNeighbour(HexDirection.SW, cells[index - CellCountX - 1]);
                } else  // 'shunted' row, always has bottom left, but last does not have bottom right
                {
                    cell.SetNeighbour(HexDirection.SW, cells[index - CellCountX]);
                    if (x != CellCountX - 1)
                        cell.SetNeighbour(HexDirection.SE, cells[index - CellCountX + 1]);
                }
            }

            var label = Instantiate<Text>(CellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.Coordinates.ToString("\n");
            cell.UIRect = label.rectTransform;

            // defaults - will trigger an initial perturb of height          
            cell.Elevation = 0;
            cell.TerrainTypeIndex = 2; // mud
            cell.WaterLevel = 1; // covered in water

            AddCellToChunk(x, z, cell);
        }

        private void AddCellToChunk(int x, int z, HexCell cell)
        {
            var chunkX = x / HexMetrics.ChunkSizeX;
            var chunkZ = z / HexMetrics.ChunkSizeZ;
            var chunk = chunks[chunkZ * chunkCountX + chunkX];

            var localX = x - chunkX * HexMetrics.ChunkSizeX; // index in chunk
            var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
            chunk.AddCell(localZ * HexMetrics.ChunkSizeX + localX, cell);
        }

        public HexCell GetCell(Vector3 position)
        {
            position = transform.InverseTransformPoint(position);
            var coords = HexCoordinates.FromPosition(position);
            return GetCell(coords);
        }

        public HexCell GetCell(HexCoordinates coords)
        {
            var index = coords.Z *CellCountX + coords.X + coords.Z/2;
            if (index < 0 || index >= cells.Length)
                return null;
            return cells[index];
        }

        public void ShowUI(bool visible)
        {
            foreach(var chunk in chunks) chunk.ShowUI(visible);
        }

        public void Save(BinaryWriter writer)
        {
            foreach(var cell in cells)
                cell.Save(writer);
        }

        public void Load(BinaryReader reader)
        {
            foreach(var cell in cells)
                cell.Load(reader);
            foreach(var chunk in chunks)
                chunk.Refresh();
        }
    }
}