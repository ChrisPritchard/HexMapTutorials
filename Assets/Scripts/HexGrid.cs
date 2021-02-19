
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

        Transform[] columns;
        HexGridChunk[] chunks;
        HexCell[] cells;
        List<HexUnit> units = new List<HexUnit>();

        HexCellShaderData cellShaderData;

        bool wrapping;
        public bool Wrapping => wrapping;
        int currentCentreColumnIndex = -1;

        private void Awake()
        {
            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);
            HexUnit.UnitPrefab = UnitPrefab;
            cellShaderData = gameObject.AddComponent<HexCellShaderData>();
            cellShaderData.Grid = this;
            CreateMap(40, 30, Wrapping);
        }

        private void OnEnable() 
        {
            if(HexMetrics.NoiseSource)
                return;

            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitialiseHashGrid(Seed);            
            HexMetrics.WrapSize = Wrapping ? CellCountX : 0;
            HexUnit.UnitPrefab = UnitPrefab;

            ResetVisibility();
        }

        public void CreateMap(int x, int z, bool wrapping)
        {
            if(x % HexMetrics.ChunkSizeX != 0
            || z % HexMetrics.ChunkSizeZ != 0)
            {
                Debug.Log("invalid size specified - must be multiples of chunksize");
                return;
            }

            this.wrapping = wrapping;
            HexMetrics.WrapSize = wrapping ? CellCountX : 0;
            this.currentCentreColumnIndex = -1;

            ClearPath();
            ClearUnits();

            if(columns != null)
                foreach(var column in columns)
                    Destroy(column.gameObject);

            chunkCountX = x / HexMetrics.ChunkSizeX;
            chunkCountZ = z / HexMetrics.ChunkSizeZ;
            cellShaderData.Initialise(CellCountX, CellCountZ);

            CreateChunks();
            CreateCells();
        }

        private void CreateChunks()
        {
            columns = new Transform[chunkCountX];
            for(var x = 0; x < chunkCountX; x++)
            {
                columns[x] = new GameObject("Column").transform;
                columns[x].SetParent(transform, false);
            }

            chunks = new HexGridChunk[chunkCountX * chunkCountZ];

            for(var z = 0; z < chunkCountZ; z++)
                for(var x = 0; x < chunkCountX; x++)
                {
                    var chunk = Instantiate<HexGridChunk>(ChunkPrefab);
                    chunk.transform.SetParent(columns[x]);

                    var index = z * chunkCountX + x;
                    chunks[index] = chunk;
                }
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
            var px = (x + z/2f - z/2) * HexMetrics.InnerDiameter;
            var pz = z * (1.5f * HexMetrics.OuterRadius);
            var position = new Vector3(px, 0f, pz);

            var cell = Instantiate<HexCell>(CellPrefab);
            cell.Index = i;
            cell.ColumnIndex = x / HexMetrics.ChunkSizeX;
            cell.transform.localPosition = position;
            cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.ShaderData = cellShaderData;

            cell.Explorable = x > 0 && z > 0 && x < CellCountX - 1 && z < CellCountZ - 1;

            var index = z * CellCountX + x;
            cells[index] = cell;

            // connect neighbours, working backwards. e.g. connect the prior, and the bottom two corners if available
            // the setneighbour function does the reverse, so connecting back will conneck the prior cell to the current one too
            // in this way, all cells are connected to their neighbours

            if (x > 0)
            {
                cell.SetNeighbour(HexDirection.W, cells[index - 1]);
                if(wrapping && x == CellCountX - 1)
                    cell.SetNeighbour(HexDirection.E, cells[i - x]);
            }
            if (z > 0)
            {            
                if ((z & 1) == 0) // non 'shunted' row, so always has bottom right, but first doesnt have bottom left
                {
                    cell.SetNeighbour(HexDirection.SE, cells[index - CellCountX]);
                    if (x > 0)
                        cell.SetNeighbour(HexDirection.SW, cells[index - CellCountX - 1]);
                    else if(wrapping)
                        cell.SetNeighbour(HexDirection.SW, cells[i - 1]);
                } 
                else  // 'shunted' row, always has bottom left, but last does not have bottom right
                {
                    cell.SetNeighbour(HexDirection.SW, cells[index - CellCountX]);
                    if (x < CellCountX - 1)
                        cell.SetNeighbour(HexDirection.SE, cells[index - CellCountX + 1]);
                    else if (wrapping)
                        cell.SetNeighbour(HexDirection.SE, cells[i - CellCountX * 2 + 1]);
                }
            }

            var label = Instantiate<Text>(CellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            cell.UIRect = label.rectTransform;
    
            //cell.Elevation = 0;
            //cell.TerrainTypeIndex = 2; // mud
            //cell.WaterLevel = 1; // covered in water

            AddCellToChunk(x, z, cell);
        }

        public void CentreMap(float xPosition)
        {
            var centreColumnIndex = (int)(xPosition / (HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX));
            if(centreColumnIndex == currentCentreColumnIndex)
                return;
            currentCentreColumnIndex = centreColumnIndex; // index of column camera is over

            // each column is at 0,0, and its chunks are relative to that
            // meaning, if the column is moved to the right edge, its chunks will be adjusted proportionally
            // and vice versa - if a column is shifted a map width back, its chunks will be in the right position a length back

            var minColumnIndex = centreColumnIndex - chunkCountX / 2; // visible columns
            var maxColumnIndex = centreColumnIndex + chunkCountX / 2;

            var position = new Vector3();
            var chunkAdjust = HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX * chunkCountX; // one map length
            for(var i = 0; i < columns.Length; i++)
            {   
                if(i < minColumnIndex)
                    position.x = chunkAdjust; // move it ahead
                else if(i > maxColumnIndex)
                    position.x = -chunkAdjust; // move it behind
                else
                    position.x = 0;
                columns[i].localPosition = position;
            }
        }

        public void AddUnit(HexUnit unit, HexCell location, float orientation)
        {
            units.Add(unit);
            unit.transform.SetParent(transform, false);
            unit.Grid = this;
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

        public HexCell GetCell(int xOffset, int zOffset) => cells[zOffset * CellCountX + xOffset];

        public HexCell GetCell(int cellIndex) => cells[cellIndex];

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

        public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            ClearPath();
            currentPathFrom = fromCell;
            currentPathTo = toCell;
            currentPathExists = Search(fromCell, toCell, unit);
            if(currentPathExists)
                ShowPath(unit.Speed);
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

        private bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
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
                var currentTurn = (current.Distance - 1) / unit.Speed;

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = current.Neighbours[(int)d];

                    if (neighbour == null || neighbour.SearchPhase > searchFrontierPhase)
                        continue;
                    if(!unit.IsValidDestination(neighbour))
                        continue;
                    var moveCost = unit.GetMoveCost(current, neighbour, d);
                    if(moveCost < 0)
                        continue;

                    var distance = current.Distance + moveCost;
                    var turn = (distance - 1) / unit.Speed;
                    if (turn > currentTurn)
                        distance = turn * unit.Speed + moveCost;

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

        private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
        {
            var visibleCells = ListPool<HexCell>.Get();

            searchFrontierPhase += 2; 

            fromCell.Distance = 0;
            fromCell.SearchPhase = searchFrontierPhase;

            if(searchFrontier == null)
                searchFrontier = new HexCellPriorityQueue();
            else
                searchFrontier.Clear();
            searchFrontier.Enqueue(fromCell);

            var fromCoordinates = fromCell.Coordinates;
            range += fromCell.ViewElevation;

            while(searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                current.SearchPhase ++;

                visibleCells.Add(current);

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = current.Neighbours[(int)d];

                    if (neighbour == null || neighbour.SearchPhase > searchFrontierPhase)
                        continue;

                    var distance = current.Distance + 1;
                    if(distance + neighbour.ViewElevation > range
                    || distance > fromCoordinates.DistanceTo(neighbour.Coordinates)
                    || !neighbour.Explorable)
                        continue;

                    if (neighbour.SearchPhase < searchFrontierPhase)
                    {
                        neighbour.SearchPhase = searchFrontierPhase;
                        neighbour.Distance = distance;
                        neighbour.SearchHeuristic = 0;
                        searchFrontier.Enqueue(neighbour);
                    } 
                    else if(distance < neighbour.Distance)
                    {
                        var oldPriority = neighbour.SearchPriority;
                        neighbour.Distance = distance;
                        searchFrontier.Change(neighbour, oldPriority);
                    }
                }
            }

            return visibleCells;
        }

        public void IncreaseVisibility(HexCell cell, int range)
        {
            var cells = GetVisibleCells(cell, range);
            cells.ForEach(c => c.IncreaseVisibility());
            ListPool<HexCell>.Add(cells);
        }

        public void DecreaseVisibility(HexCell cell, int range)
        {
            var cells = GetVisibleCells(cell, range);
            cells.ForEach(c => c.DecreaseVisibility());
            ListPool<HexCell>.Add(cells);
        }

        public void ResetVisibility()
        {
            foreach(var cell in cells)   
                cell.ResetVisibility();
            foreach(var unit in units)
                IncreaseVisibility(unit.Location, unit.VisionRange);
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(CellCountX);
            writer.Write(CellCountZ);
            writer.Write(Wrapping);

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
            var wrapping = reader.ReadBoolean();

            CreateMap(cX, cY, wrapping); // ensures the right size max is created

            var mode = cellShaderData.ImmediateMode;
            cellShaderData.ImmediateMode = true; // visibility is shown immediately on initial load

            foreach(var cell in cells)
                cell.Load(reader);
            foreach(var chunk in chunks)
                chunk.Refresh();

            var unitCount = reader.ReadInt32();
            for(var i = 0; i < unitCount; i++)
                HexUnit.Load(reader, this);

            cellShaderData.ImmediateMode = mode;
        }
    }
}