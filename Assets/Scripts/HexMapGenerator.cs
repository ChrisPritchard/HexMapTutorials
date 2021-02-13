
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexMapGenerator : MonoBehaviour 
    {
        public HexGrid Grid;

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


        private int cellCount;
        private HexCellPriorityQueue searchFrontier;
        private int searchFrontierPhase;

        public void GenerateMap (int x, int z)
        {
            cellCount = x * z;
            Grid.CreateMap(x, z);

            if(searchFrontier == null)
                searchFrontier = new HexCellPriorityQueue();

            for(var i = 0; i < cellCount; i++)
                Grid.GetCell(i).WaterLevel = (byte)WaterLevel;

            CreateLand();
            SetTerrainType();

            for(var i = 0; i < cellCount; i++)
                Grid.GetCell(i).SearchPhase = 0;
        }

        private void CreateLand()
        {
            var landBudget = Mathf.RoundToInt(cellCount * LandPercentage * 0.01f);

            while(landBudget > 0)
            {
                var chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax + 1);
                if(Random.value < SinkProbability)
                    landBudget = SinkTerrain(chunkSize, landBudget);
                else
                    landBudget = RaiseTerrain(chunkSize, landBudget);
            }
        }

        private void SetTerrainType()
        {
            for(var i = 0; i < cellCount; i++)
            {
                var cell = Grid.GetCell(i);
                if(!cell.IsUnderwater)
                    cell.TerrainTypeIndex = (byte)Mathf.Clamp(cell.Elevation - cell.WaterLevel, 0, 255);
            }
        }

        private int RaiseTerrain(int chunkSize, int budget)
        {
            searchFrontierPhase ++;

            var firstCell = GetRandomCell();
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

        private int SinkTerrain(int chunkSize, int budget)
        {
            searchFrontierPhase ++;

            var firstCell = GetRandomCell();
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

        private HexCell GetRandomCell()
        {
            return Grid.GetCell(Random.Range(0, cellCount));
        }
    }
}