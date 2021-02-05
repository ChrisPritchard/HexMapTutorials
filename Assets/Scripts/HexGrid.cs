
namespace DarkDomains
{
    using System.Collections;
    using System.Collections.Generic;
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

            CreateMap(8, 6);
        }

        private void OnEnable() 
        {
            if(HexMetrics.NoiseSource)
                return;

            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);
        }

        public void CreateMap(int xChunks, int zChunks)
        {
            if(chunks != null)
                foreach(var chunk in chunks)
                    Destroy(chunk.gameObject);

            chunkCountX = xChunks;
            chunkCountZ = zChunks;

            CreateChunks();
            CreateCells();
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
            foreach(var chunk in chunks) 
                chunk.ShowUI(visible);
        }

        public void FindPath(HexCell fromCell, HexCell toCell)
        {
            StopAllCoroutines();
            StartCoroutine(Search(fromCell, toCell));
        }

        IEnumerator Search(HexCell fromCell, HexCell toCell)
        {
            for(var i = 0; i < cells.Length; i++)
            {
                cells[i].Distance = int.MaxValue;
                cells[i].DisableHighlight();
            }
            fromCell.EnableHighlight(Color.blue);
            toCell.EnableHighlight(Color.red);

            var delay = new WaitForSeconds(1 / 60f);

            fromCell.Distance = 0;
            var frontier = new List<HexCell> { fromCell };
                        
            while(frontier.Count > 0)
            {
                yield return delay;

                var current = frontier[0];
                frontier.RemoveAt(0);

                if(current == toCell)
                {
                    current = current.PathFrom;
                    while(current != fromCell)
                    {
                        current.EnableHighlight(Color.white);
                        current = current.PathFrom;
                    }
                    break;
                }

                for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = current.Neighbours[(int)d];

                    if(neighbour == null || neighbour.IsUnderwater)
                        continue;

                    var edgeType = current.GetEdgeType(neighbour);
                    if(edgeType == HexEdgeType.Cliff)
                        continue;

                    var distance = 10;
                    if(current.HasRoadThroughEdge(d))
                        distance = 1;
                    else if(current.Walled != neighbour.Walled)
                        continue;
                    else
                    {
                        if (edgeType == HexEdgeType.Flat)
                            distance = 5;
                        distance += neighbour.UrbanLevel + neighbour.FarmLevel + neighbour.ForestLevel;
                    }
                    var newDistance = current.Distance + distance;

                    if(neighbour.Distance == int.MaxValue)
                    {
                        neighbour.Distance = newDistance;
                        neighbour.PathFrom = current;
                        neighbour.SearchHeuristic = neighbour.Coordinates.DistanceTo(toCell.Coordinates);
                        frontier.Add(neighbour);
                    } 
                    else if(newDistance < neighbour.Distance)
                    {
                        neighbour.Distance = newDistance;
                        neighbour.PathFrom = current;
                    }
                }

                frontier.Sort((a, b) => a.SearchPriority.CompareTo(b.SearchPriority));
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(chunkCountX);
            writer.Write(chunkCountZ);

            foreach(var cell in cells)
                cell.Save(writer);
        }

        public void Load(BinaryReader reader)
        {
            StopAllCoroutines();
            var cX = reader.ReadInt32();
            var cY = reader.ReadInt32();
            CreateMap(cX, cY); // ensures the right size max is created

            foreach(var cell in cells)
                cell.Load(reader);
            foreach(var chunk in chunks)
                chunk.Refresh();
        }
    }
}