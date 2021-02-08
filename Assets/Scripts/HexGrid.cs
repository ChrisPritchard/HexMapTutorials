
namespace DarkDomains
{
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
        public HexUnit UnitPrefab;

        public int Seed;

        HexGridChunk[] chunks;
        HexCell[] cells;
        List<HexUnit> units = new List<HexUnit>();

        HexCellShaderData cellShaderData;

        private void Awake()
        {
            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);
            HexUnit.UnitPrefab = UnitPrefab;
            cellShaderData = gameObject.AddComponent<HexCellShaderData>();
            CreateMap(8, 6);
        }

        private void OnEnable() 
        {
            if(HexMetrics.NoiseSource)
                return;

            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);
            HexUnit.UnitPrefab = UnitPrefab;
        }

        public void CreateMap(int xChunks, int zChunks)
        {
            ClearPath();
            ClearUnits();

            if(chunks != null)
                foreach(var chunk in chunks)
                    Destroy(chunk.gameObject);

            chunkCountX = xChunks;
            chunkCountZ = zChunks;
            cellShaderData.Initialise(CellCountX, CellCountZ);

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

            var index = z * chunkCountX + x;
            chunks[index] = chunk;
        }

        private void CreateCells()
        {
            cells = new HexCell[CellCountX * CellCountZ];

            var i = 0;
            for(var z = 0; z < CellCountZ; z++)
                for(var x = 0; x < CellCountX; x++)
                    CreateCell(x, z, i++);
        }

        private void CreateCell(int x, int z, int i)
        {
            var px = (x + z/2f - z/2) * (2 * HexMetrics.InnerRadius);
            var pz = z * (1.5f * HexMetrics.OuterRadius);
            var position = new Vector3(px, 0f, pz);

            var cell = Instantiate<HexCell>(CellPrefab);
            cell.Index = i;
            cell.transform.localPosition = position;
            cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.ShaderData = cellShaderData;

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

        public void AddUnit(HexUnit unit, HexCell location, float orientation)
        {
            units.Add(unit);
            unit.transform.SetParent(transform, false);
            unit.Location = location;
            unit.Orientation = orientation;
        }

        public void RemoveUnit(HexUnit unit)
        {
            units.Remove(unit);
            unit.Die();
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

        public HexCell GetCell(Ray ray) 
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
                return GetCell(hit.point);
            return null;
        }

        public HexCell GetCell(HexCoordinates coords)
        {
            var index = coords.Z *CellCountX + coords.X + coords.Z/2;
            if (index < 0 || index >= cells.Length)
                return null;
            return cells[index];
        }

        private void ClearUnits()
        {
            foreach(var unit in units)
                unit.Die();
            units.Clear();
        }

        public void ShowUI(bool visible)
        {
            foreach(var chunk in chunks) 
                chunk.ShowUI(visible);
        }

        HexCellPriorityQueue searchFrontier;
        int searchFrontierPhase;
        HexCell currentPathFrom, currentPathTo;
        bool currentPathExists;

        public void FindPath(HexCell fromCell, HexCell toCell, int speed)
        {
            ClearPath();
            currentPathFrom = fromCell;
            currentPathTo = toCell;
            currentPathExists = Search(fromCell, toCell, speed);
            if(currentPathExists)
                ShowPath(speed);
        }

        private void ShowPath(int speed)
        {
            if(currentPathExists)
            {
                var current = currentPathTo;
                while(current != currentPathFrom)
                {
                    var turn = (current.Distance - 1) / speed;
                    current.SetLabel(turn.ToString());
                    current.EnableHighlight(Color.white);
                    current = current.PathFrom;
                }
            }
            currentPathFrom.EnableHighlight(Color.blue);
            currentPathTo.EnableHighlight(Color.red);
        }

        public void ClearPath()
        {
            if(currentPathExists)
            {
                var current = currentPathTo;
                while(current != currentPathFrom)
                {
                    current.SetLabel("");
                    current.DisableHighlight();
                    current = current.PathFrom;
                }
                current.DisableHighlight();
                currentPathExists = false;
            }
            else if (currentPathFrom)
            {
                currentPathFrom.DisableHighlight();
                currentPathTo.DisableHighlight();
            }
            currentPathFrom = currentPathTo = null;
        }

        public bool HasPath => currentPathExists;

        public List<HexCell> GetPath()
        {
            if(!currentPathExists)
                return null;
            var path = ListPool<HexCell>.Get();
            for(var c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
                path.Add(c);
            path.Add(currentPathFrom);
            path.Reverse();
            return path;
        }

        private bool Search(HexCell fromCell, HexCell toCell, int speed)
        {
            searchFrontierPhase += 2; 

            fromCell.Distance = 0;
            fromCell.SearchPhase = searchFrontierPhase;

            if(searchFrontier == null)
                searchFrontier = new HexCellPriorityQueue();
            else
                searchFrontier.Clear();
            searchFrontier.Enqueue(fromCell);

            while(searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                if(current == toCell)
                    return true;

                current.SearchPhase ++;
                var currentTurn = (current.Distance - 1) / speed;

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = current.Neighbours[(int)d];

                    if (neighbour == null || neighbour.SearchPhase > searchFrontierPhase)
                        continue;
                    if (neighbour.IsUnderwater || neighbour.Unit)
                        continue;

                    var edgeType = current.GetEdgeType(neighbour);
                    if (edgeType == HexEdgeType.Cliff)
                        continue;

                    var moveCost = 10;
                    if (current.HasRoadThroughEdge(d))
                        moveCost = 1;
                    else if(current.Walled != neighbour.Walled)
                        continue;
                    else
                    {
                        if (edgeType == HexEdgeType.Flat)
                            moveCost = 5;
                        moveCost += neighbour.UrbanLevel + neighbour.FarmLevel + neighbour.ForestLevel;
                    }

                    var distance = current.Distance + moveCost;
                    var turn = (distance - 1) / speed;
                    if (turn > currentTurn)
                        distance = turn * speed + moveCost;

                    if (neighbour.SearchPhase < searchFrontierPhase)
                    {
                        neighbour.SearchPhase = searchFrontierPhase;
                        neighbour.Distance = distance;
                        neighbour.PathFrom = current;
                        neighbour.SearchHeuristic = neighbour.Coordinates.DistanceTo(toCell.Coordinates);
                        searchFrontier.Enqueue(neighbour);
                    } 
                    else if(distance < neighbour.Distance)
                    {
                        var oldPriority = neighbour.SearchPriority;
                        neighbour.Distance = distance;
                        neighbour.PathFrom = current;
                        searchFrontier.Change(neighbour, oldPriority);
                    }
                }
            }

            return false;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(chunkCountX);
            writer.Write(chunkCountZ);

            foreach(var cell in cells)
                cell.Save(writer);

            writer.Write(units.Count);
            foreach(var unit in units)
                unit.Save(writer);
        }

        public void Load(BinaryReader reader)
        {
            ClearPath();
            ClearUnits();

            var cX = reader.ReadInt32();
            var cY = reader.ReadInt32();
            CreateMap(cX, cY); // ensures the right size max is created

            foreach(var cell in cells)
                cell.Load(reader);
            foreach(var chunk in chunks)
                chunk.Refresh();

            var unitCount = reader.ReadInt32();
            for(var i = 0; i < unitCount; i++)
                HexUnit.Load(reader, this);
        }
    }
}