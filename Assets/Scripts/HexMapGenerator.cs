
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexMapGenerator : MonoBehaviour 
    {
        public HexGrid Grid;

        public void GenerateMap (int x, int z)
        {
            Grid.CreateMap(x, z);
            for(var i = 0; i < z; i++)
                Grid.GetCell(x / 2, i).TerrainTypeIndex = 1;
        }
    }
}