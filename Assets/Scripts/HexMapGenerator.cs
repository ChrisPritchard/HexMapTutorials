
namespace DarkDomains
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum HemisphereMode { North, South, Both }

    public class HexMapGenerator : MonoBehaviour
    {
        struct MapRegion
        {
            public int XMin, XMax, ZMin, ZMax;

            public static MapRegion Create(int xmin, int xmax, int zmin, int zmax) => 
                new MapRegion { XMin = xmin, XMax = xmax, ZMin = zmin, ZMax = zmax };
        }

        struct ClimateData
        {
            public float Clouds, Moisture;
        }

        struct Biome
        {
            public int Terrain, Forest;

            public Biome(int terrain, int forest) => 
                (Terrain, Forest) = (terrain, forest);
        }

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

        public HexDirection WindDirection = HexDirection.NW;

        [Range(1f, 10f)]
        public float WindStrength = 4f;

        [Range(0f, 1f)]
        public float StartingMoisture = 0.1f;
        
        [Range(1, 20)]
        public int RiverPercentage = 10;

        [Range(0f, 1f)]
        public float ExtraLakeProbability = 0.25f;

        [Range(0f, 1f)]
        public float LowTemperature = 0f;

        [Range(0f, 1f)]
        public float HighTemperature = 1f;

        [Range(0f, 1f)]
        public float TemperatureJitter = 0.1f;

        public HemisphereMode HemisphereMode = HemisphereMode.Both;

        private int cellCount, landCells;
        private HexCellPriorityQueue searchFrontier;
        private int searchFrontierPhase;
        private List<MapRegion> regions;
        private List<ClimateData> climate = new List<ClimateData>();
        private List<ClimateData> nextClimate = new List<ClimateData>();
        private List<HexDirection> flowDirections = new List<HexDirection>();
        private int temperatureJitterChannel;

        private static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };
        private static float[] moistureBands = { 0.12f, 0.28f, 0.85f };

        // down = increasing temperature, right = increasing moisture
        private static Biome[] biomes = {
            new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0), // desert   snow    snow    snow
            new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2), // desert   mud     mud     mud
            new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2), // desert   grass   grass   grass
            new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)  // desert   grass   grass   grass
        };

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
                Grid.GetCell(i).WaterLevel = WaterLevel;

            CreateRegions();
            CreateLand();
            ErodeLand();
            CreateClimate();
            CreateRivers();
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
            landCells = landBudget;

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
            {
                Debug.Log("Failed to use up landbudget. Remaining: " + landBudget);
                landCells -= landBudget;
            }
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
            nextClimate.Clear();
            var initialData = new ClimateData { Moisture = StartingMoisture };
            var clearData = new ClimateData();

            for(var i = 0; i < cellCount; i++)
            {
                climate.Add(initialData);
                nextClimate.Add(clearData);
            }

            for(var cycle = 0; cycle < 40; cycle++)
            {
                for(var i = 0; i < cellCount; i++)
                    EvolveClimate(i);
                var swap = climate;
                climate = nextClimate;
                nextClimate = swap;
            }
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

            var cloudMaximum = 1f - cell.ViewElevation / (ElevationMaximum + 1f);
            if(cellClimate.Clouds > cloudMaximum)
            {
                cellClimate.Moisture += cellClimate.Clouds - cloudMaximum;
                cellClimate.Clouds = cloudMaximum;
            }

            var mainDispersalDirection = WindDirection.Opposite();
            var cloudDispersal = cellClimate.Clouds / (5f + WindStrength);

            var runoff = cellClimate.Moisture * RunoffFactor / 6f;
            var seepage = cellClimate.Moisture * SeepageFactor / 6f;

            for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbour = cell.GetNeighbour(d);
                if(!neighbour)
                    continue;

                var neighbourClimate = nextClimate[neighbour.Index];

                if(d == mainDispersalDirection)
                    neighbourClimate.Clouds += cloudDispersal * WindStrength;
                else
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

                nextClimate[neighbour.Index] = neighbourClimate;
            }

            var nextCellClimate = nextClimate[cellIndex];
            nextCellClimate.Moisture += cellClimate.Moisture;
            if(nextCellClimate.Moisture > 1f)
                nextCellClimate.Moisture = 1f;
            nextClimate[cellIndex] = nextCellClimate;
            climate[cellIndex] = new ClimateData();;
        }

        private void CreateRivers()
        {
            var riverOrigins = ListPool<HexCell>.Get();

            for(var i = 0; i < cellCount; i++)
            {
                var cell = Grid.GetCell(i);
                if(cell.IsUnderwater)
                    continue;
                var data = climate[i];
                var weight = data.Moisture * (float)(cell.Elevation - WaterLevel) / (ElevationMaximum - WaterLevel);
                if(weight > 0.75f)
                {
                    riverOrigins.Add(cell);
                    riverOrigins.Add(cell);
                }
                else if(weight > 0.5f)
                    riverOrigins.Add(cell);
                else if(weight > 0.25f)
                    riverOrigins.Add(cell);
            }

            var riverBudget = Mathf.RoundToInt(landCells * RiverPercentage * 0.01f);

            while(riverBudget > 0 && riverOrigins.Count > 0)
            {
                var count = riverOrigins.Count;
                var index = Random.Range(0, count);
                var origin = riverOrigins[index];
                riverOrigins[index] = riverOrigins[count - 1];
                riverOrigins.RemoveAt(count - 1);

                if(!origin.HasRiver)
                {
                    riverBudget -= CreateRiver(origin);

                    var isValidOrigin = true;
                    for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                    {
                        var neighbour = origin.GetNeighbour(d);
                        if(neighbour && (neighbour.HasIncomingRiver || neighbour.IsUnderwater))
                        {
                            isValidOrigin = false;
                            break;
                        }
                    }
                    if(isValidOrigin)
                        riverBudget -= CreateRiver(origin);
                }
            }

            if(riverBudget > 0)
            {
                Debug.Log("Failed to use up riverbudget. Remaining: " + riverBudget);
            }

            ListPool<HexCell>.Add(riverOrigins);
        }

        private int CreateRiver(HexCell origin)
        {
            var length = 1;
            var cell = origin;
            var direction = HexDirection.NE;

            while(!cell.IsUnderwater)
            {
                var minNeighbourElevation = int.MaxValue;
                flowDirections.Clear();

                for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbour = cell.GetNeighbour(d);

                    if(!neighbour)
                        continue;

                    if(neighbour.Elevation < minNeighbourElevation)
                        minNeighbourElevation = neighbour.Elevation;

                    if(neighbour == origin || neighbour.HasIncomingRiver)
                        continue;

                    var delta = neighbour.Elevation - cell.Elevation;
                    if(delta > 0)
                        continue;

                    if(neighbour.HasOutgoingRiver)
                    {
                        cell.SetOutgoingRiver(d); // found a river origin, so merge
                        return length;
                    }

                    if(delta < 0) // downhill rivers are more likely
                    {
                        flowDirections.Add(d);
                        flowDirections.Add(d);
                        flowDirections.Add(d);
                    }
                    if(d != direction.Previous2() && d != direction.Next2())
                        flowDirections.Add(d); // if straight to one to the left or right, more likely than a sharp turn

                    flowDirections.Add(d);
                }

                if(flowDirections.Count == 0)
                {
                    if(length == 1)
                        return 0;
                        
                    if(minNeighbourElevation >= cell.Elevation) // create a lake
                    {
                        cell.WaterLevel = minNeighbourElevation;
                        if(minNeighbourElevation == cell.Elevation)
                            cell.Elevation = minNeighbourElevation - 1;
                    }
                    break;
                }

                direction = flowDirections[Random.Range(0, flowDirections.Count)];
                cell.SetOutgoingRiver(direction);
                length++;

                if(minNeighbourElevation >= cell.Elevation && Random.value < ExtraLakeProbability) // create a lake
                {
                    cell.WaterLevel = cell.Elevation;
                    cell.Elevation = cell.Elevation - 1;
                }

                cell = cell.GetNeighbour(direction);
            }

            return length;
        }

        private void SetTerrainType()
        {
            temperatureJitterChannel = Random.Range(0, 4);
            var rockDesertElevation = (ElevationMaximum - WaterLevel) / 2;

            for(var i = 0; i < cellCount; i++)
            {
                var cell = Grid.GetCell(i);
                var temperature = DetermineTemperature(cell);
                var moisture = climate[i].Moisture;

                if(!cell.IsUnderwater)
                {
                    var t = 0;
                    for (; t < temperatureBands.Length; t++)
                        if(temperature < temperatureBands[t])
                            break;
                    var m = 0;
                    for (; m < moistureBands.Length; m++)
                        if(moisture < moistureBands[m])
                            break;

                    var biome = biomes[t * 4 + m];
                    if(biome.Terrain == 0)
                    {
                        if(cell.Elevation - WaterLevel > rockDesertElevation)
                            biome.Terrain = 3;
                    }
                    else if(cell.Elevation == ElevationMaximum)
                        biome.Terrain = 4;

                    if(biome.Terrain == 4)
                        biome.Forest = 0;
                    if(biome.Forest < 3 && cell.HasRiver)
                        biome.Forest ++;

                    cell.TerrainTypeIndex = biome.Terrain;
                    cell.ForestLevel = (byte)biome.Forest;
                }
                else
                {
                    var terrain = 2;
                    if(cell.Elevation == WaterLevel - 1)
                    {
                        // possible coast
                        int cliffs = 0, slopes = 0;
                        for(var d = HexDirection.NE; d <= HexDirection.NW; d++)
                        {
                            var neighbour = cell.GetNeighbour(d);
                            if(!neighbour)
                                continue;
                            var delta = neighbour.Elevation - cell.WaterLevel;
                            if(delta == 0)
                                slopes++;
                            else if(delta > 0)
                                cliffs++;
                            if (cliffs + slopes > 3)
                                terrain = 1; // grass
                            else if(cliffs > 0)
                                terrain = 3; // rock
                            else if(slopes > 0)
                                terrain = 0; // sand
                            else
                                terrain = 1; // grass
                        }
                    }
                    else if(cell.Elevation >= WaterLevel)
                        terrain = 1; // grass
                    else if(cell.Elevation < 0)
                        terrain = 3; // rock
                    if (terrain == 1 && temperature < temperatureBands[0])
                        terrain = 2; // mud
                    cell.TerrainTypeIndex = terrain;
                }
            }
        }

        private float DetermineTemperature(HexCell cell)
        {
            var latitude = (float)cell.Coordinates.Z / Grid.CellCountZ;

            if(HemisphereMode == HemisphereMode.Both)
            {
                latitude *= 2f;
                if(latitude > 1f)
                    latitude = 2f - latitude;
            } 
            else if (HemisphereMode == HemisphereMode.North)
                latitude = 1f - latitude;

            var temperature = Mathf.LerpUnclamped(LowTemperature, HighTemperature, latitude);

            // cool by height
            temperature *= 1f - (cell.ViewElevation - WaterLevel) / (ElevationMaximum - WaterLevel + 1f);

            var jitter = HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];
            temperature += (jitter * 2f - 1f) * TemperatureJitter;

            return temperature;
        }
    }
}