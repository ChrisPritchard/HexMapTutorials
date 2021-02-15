
namespace DarkDomains
{
    using System.Collections.Generic;
    using UnityEngine;

    public struct MapRegion
    {
        public int XMin, XMax, ZMin, ZMax;

        public static MapRegion Create(int xmin, int xmax, int zmin, int zmax) => 
            new MapRegion { XMin = xmin, XMax = xmax, ZMin = zmin, ZMax = zmax };
    }

    public struct ClimateData
    {
        public float Clouds, Moisture;
    }

    public class HexMapGenerator : MonoBehaviour
    {
        public HexGrid Grid;

        public int Seed;

        public bool UseFixedSeed;

        [Range(0f, 0.5f)]
        public float JitterProbability = 0.25f;

        [Range(0f, 1f)]
        public float HighRiseProbability = 0.25f;

        [Range(0f, 0.4f)]
        public float SinkProbability = 0.2f;

        [Range(20, 200)]
        public int ChunkSizeMin = 30;

        [Range(20, 200)]
        public int ChunkSizeMax = 100;

        [Range(5, 95)]
        public int LandPercentage = 50;

        [Range(1, 5)]
        public int WaterLevel = 3;

        [Range(-4, 0)]
        public int ElevationMinimum = -2;

        [Range(6, 10)]
        public int ElevationMaximum = 8;

        [Range(0, 10)]
        public int MapBorderX = 5;

        [Range(0, 10)]
        public int MapBorderZ = 5;

        [Range(0, 10)]
        public int RegionBorder = 5;

        [Range(1, 4)]
        public int RegionCount = 1;

        [Range(0, 100)]
        public int ErosionPercentage = 50;

        [Range(0f, 1f)]
        public float Evaporation = 0.5f;

        [Range(0f, 1f)]
        public float PrecipitationFactor = 0.25f;

        [Range(0f, 1f)]
        public float EvaporationFactor = 0.5f;

        [Range(0f, 1f)]
        public float RunoffFactor = 0.25f;

        [Range(0f, 1f)]
        public float SeepageFactor = 0.125f;

        private int cellCount;
        private HexCellPriorityQueue searchFrontier;
        private int searchFrontierPhase;
        private List<MapRegion> regions;
        private List<ClimateData> climate = new List<ClimateData>();

        public void GenerateMap (int x, int z)
        {
            var originalRandomState = Random.state;
            if(!UseFixedSeed)
                Seed = Random.Range(0, int.MaxValue);
            Random.InitState(Seed);

            cellCount = x * z;
            Grid.CreateMap(x, z);

            if(searchFrontier == null)
                searchFrontier = new HexCellPriorityQueue();

            for(var i = 0; i < cellCount; i++)
                Grid.GetCell(i).WaterLevel = (byte)WaterLevel;

            CreateRegions();
            CreateLand();
            ErodeLand();
            CreateClimate();
            SetTerrainType();

            for(var i = 0; i < cellCount; i++)
                Grid.GetCell(i).SearchPhase = 0;
        }

        private void CreateRegions()
        {
            if(regions == null)
                regions = new List<MapRegion>();
            else
                regions.Clear();

            if (RegionCount == 1)
                regions.Add(MapRegion.Create(MapBorderX, Grid.CellCountX - MapBorderX, MapBorderZ, Grid.CellCountZ - MapBorderZ));
            else if (RegionCount == 2 && Random.value < 0.5f) // horizonal split
                regions.AddRange(new [] {
                    MapRegion.Create(MapBorderX, Grid.CellCountX / 2 - RegionBorder, MapBorderZ, Grid.CellCountZ - MapBorderZ),
                    MapRegion.Create(Grid.CellCountX / 2 + RegionBorder, Grid.CellCountX - MapBorderX, MapBorderZ, Grid.CellCountZ - MapBorderZ)
                });
            else if (RegionCount == 2) // vertical split
                regions.AddRange(new [] {
                    MapRegion.Create(MapBorderX, Grid.CellCountX - MapBorderX, MapBorderZ, Grid.CellCountZ / 2 - RegionBorder),
                    MapRegion.Create(MapBorderX, Grid.CellCountX - MapBorderX, Grid.CellCountZ / 2 + RegionBorder, Grid.CellCountZ - MapBorderZ)
                });
            else if (RegionCount == 3)
                regions.AddRange(new [] {
                    MapRegion.Create(MapBorderX, Grid.CellCountX / 3 - RegionBorder, MapBorderZ, Grid.CellCountZ - MapBorderZ),
                    MapRegion.Create(Grid.CellCountX / 3 + RegionBorder, Grid.CellCountX * 2 / 3 - RegionBorder, MapBorderZ, Grid.CellCountZ - MapBorderZ),
                    MapRegion.Create(Grid.CellCountX * 2 / 3 + RegionBorder, Grid.CellCountX - MapBorderX, MapBorderZ, Grid.CellCountZ - MapBorderZ)
                });
            else 
                regions.AddRange(new [] {
                    MapRegion.Create(MapBorderX, Grid.CellCountX / 2 - RegionBorder, MapBorderZ, Grid.CellCountZ / 2 - RegionBorder),
                    MapRegion.Create(Grid.CellCountX / 2 + RegionBorder, Grid.CellCountX - MapBorderX, MapBorderZ, Grid.CellCountZ / 2 - RegionBorder),
                    MapRegion.Create(MapBorderX, Grid.CellCountX / 2 - RegionBorder, Grid.CellCountZ / 2 + RegionBorder, Grid.CellCountZ - MapBorderZ),
                    MapRegion.Create(Grid.CellCountX / 2 + RegionBorder, Grid.CellCountX - MapBorderX, Grid.CellCountZ / 2 + RegionBorder, Grid.CellCountZ - MapBorderZ)
                });
        }

        private void CreateLand()
        {
            var landBudget = Mathf.RoundToInt(cellCount * LandPercentage * 0.01f);

            for(var guard = 0; landBudget > 0 && guard < 10000; guard++) // guard prevents against infinite loops
            {
                var sink = Random.value < SinkProbability;
                for(var i = 0; i < regions.Count; i++)
                {
                    var chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax + 1);
                    if(sink)
                        landBudget = SinkTerrain(chunkSize, landBudget, regions[i]);
                    else
                        landBudget = RaiseTerrain(chunkSize, landBudget, regions[i]);
                    if(landBudget == 0)
                        return;
                }
            }

            if(landBudget > 0)
                Debug.Log("Failed to use up landbudget. Remaining: " + landBudget);
        }

        private int RaiseTerrain(int chunkSize, int budget, MapRegion region)
        {
            searchFrontierPhase ++;

            var firstCell = GetRandomCell(region);
            firstCell.SearchPhase = searchFrontierPhase;
            firstCell.Distance = 0;
            firstCell.SearchHeuristic = 0;
            searchFrontier.Enqueue(firstCell);

            var centre = firstCell.Coordinates;

            var rise = Random.value < HighRiseProbability ? 2 : 1;
            var size = 0;
            while (size < chunkSize && searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                var originalElevation = current.Elevation;
                var newElevation = originalElevation + rise;

                if(newElevation > ElevationMaximum)
                    continue; // skip high point and its neighbours, so we grow around peaks

                current.Elevation = newElevation;
                if(originalElevation < WaterLevel && newElevation >= WaterLevel && --budget == 0)
                    break;
                size ++;

                for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = current.GetNeighbour(d);
                    if(neighbour && neighbour.SearchPhase < searchFrontierPhase)
                    {
                        neighbour.SearchPhase = searchFrontierPhase;
                        neighbour.Distance = neighbour.Coordinates.DistanceTo(centre);
                        neighbour.SearchHeuristic = Random.value < JitterProbability ? 1 : 0;
                        searchFrontier.Enqueue(neighbour);
                    }
                }
            }
            searchFrontier.Clear();
            return budget;
        }

        private int SinkTerrain(int chunkSize, int budget, MapRegion region)
        {
            searchFrontierPhase ++;

            var firstCell = GetRandomCell(region);
            firstCell.SearchPhase = searchFrontierPhase;
            firstCell.Distance = 0;
            firstCell.SearchHeuristic = 0;
            searchFrontier.Enqueue(firstCell);

            var centre = firstCell.Coordinates;

            var sink = Random.value < SinkProbability ? 2 : 1;
            var size = 0;
            while (size < chunkSize && searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                var originalElevation = current.Elevation;
                var newElevation = originalElevation - sink;

                if(newElevation < ElevationMinimum)
                    continue;

                current.Elevation = newElevation;
                if(originalElevation >= WaterLevel && newElevation < WaterLevel)
                    budget++;
                size ++;

                for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = current.GetNeighbour(d);
                    if(neighbour && neighbour.SearchPhase < searchFrontierPhase)
                    {
                        neighbour.SearchPhase = searchFrontierPhase;
                        neighbour.Distance = neighbour.Coordinates.DistanceTo(centre);
                        neighbour.SearchHeuristic = Random.value < JitterProbability ? 1 : 0;
                        searchFrontier.Enqueue(neighbour);
                    }
                }
            }
            searchFrontier.Clear();
            return budget;
        }

        private HexCell GetRandomCell(MapRegion region) => 
            Grid.GetCell(Random.Range(region.XMin, region.XMax), Random.Range(region.ZMin, region.ZMax));

        private void ErodeLand()
        {
            var erodibleCells = ListPool<HexCell>.Get();

            for(var i = 0; i < cellCount; i++)
            {
                var cell = Grid.GetCell(i);
                if(IsErodible(cell))
                    erodibleCells.Add(cell);
            }

            var targetErodibleCount = (int)(erodibleCells.Count * (100 - ErosionPercentage) * 0.01f);

            while(erodibleCells.Count > targetErodibleCount)
            {
                var index = Random.Range(0, erodibleCells.Count);
                var cell = erodibleCells[index];
                var target = GetErosionTarget(cell);

                cell.Elevation --;
                target.Elevation ++;

                if(!IsErodible(cell))
                {
                    erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                    erodibleCells.RemoveAt(erodibleCells.Count - 1);
                }

                for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = cell.GetNeighbour(d);
                    if(neighbour && neighbour.Elevation == cell.Elevation + 2 
                    && !erodibleCells.Contains(neighbour))
                        erodibleCells.Add(neighbour);
                }

                if(IsErodible(target) && !erodibleCells.Contains(target))
                    erodibleCells.Add(target);

                for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = target.GetNeighbour(d);
                    if(neighbour && neighbour != cell 
                    && neighbour.Elevation == target.Elevation + 1 
                    && !IsErodible(neighbour) && erodibleCells.Contains(neighbour))
                        erodibleCells.Remove(neighbour);
                }
            }

            ListPool<HexCell>.Add(erodibleCells);
        }

        private bool IsErodible(HexCell cell)
        {
            var erodibleElevation = cell.Elevation - 2;
            for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbour = cell.GetNeighbour(d);
                if(neighbour && neighbour.Elevation <= erodibleElevation)
                    return true;
            }
            return false;
        }

        private HexCell GetErosionTarget(HexCell cell)
        {
            var candidates = ListPool<HexCell>.Get();

            var erodibleElevation = cell.Elevation - 2;
            for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbour = cell.GetNeighbour(d);
                if(neighbour && neighbour.Elevation <= erodibleElevation)
                    candidates.Add(neighbour);
            }

            var candidate = candidates[Random.Range(0, candidates.Count)];
            ListPool<HexCell>.Add(candidates);
            return candidate;
        }

        private void CreateClimate()
        {
            climate.Clear();
            var initialData = new ClimateData();

            for(var i = 0; i < cellCount; i++)
                climate.Add(initialData);

            for(var cycle = 0; cycle < 40; cycle++)
                for(var i = 0; i < cellCount; i++)
                    EvolveClimate(i);
        }

        private void EvolveClimate(int cellIndex)
        {
            var cell = Grid.GetCell(cellIndex);
            var cellClimate = climate[cellIndex];

            if(cell.IsUnderwater)
            {
                cellClimate.Moisture = 1f;
                cellClimate.Clouds += Evaporation;
            }
            else
            {
                var evaporation = cellClimate.Moisture * EvaporationFactor;
                cellClimate.Moisture -= evaporation;
                cellClimate.Clouds += evaporation;
            }

            var precipitation = cellClimate.Clouds * PrecipitationFactor;
            cellClimate.Clouds -= precipitation;
            cellClimate.Moisture += precipitation;

            var cloudDispersal = cellClimate.Clouds / 6f;
            var runoff = cellClimate.Moisture * RunoffFactor / 6f;
            var seepage = cellClimate.Moisture * SeepageFactor / 6f;

            for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbour = cell.GetNeighbour(d);
                if(!neighbour)
                    continue;

                var neighbourClimate = climate[neighbour.Index];
                neighbourClimate.Clouds += cloudDispersal;

                var elevationDelta = neighbour.ViewElevation - cell.ViewElevation;
                if(elevationDelta < 0)
                {
                    cellClimate.Moisture -= runoff;
                    neighbourClimate.Moisture += runoff;
                } 
                else if(elevationDelta == 0)
                {
                    cellClimate.Moisture -= seepage;
                    neighbourClimate.Moisture += seepage;
                }

                climate[neighbour.Index] = neighbourClimate;
            }
            cellClimate.Clouds = 0f;
            
            climate[cellIndex] = cellClimate;
        }

        private void SetTerrainType()
        {
            for(var i = 0; i < cellCount; i++)
            {
                var cell = Grid.GetCell(i);
                if(!cell.IsUnderwater)
                    cell.TerrainTypeIndex = (byte)Mathf.Clamp(cell.Elevation - cell.WaterLevel, 0, 255);

                cell.SetMapData(climate[i].Moisture);
            }
        }
    }
}