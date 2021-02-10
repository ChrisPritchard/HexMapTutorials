
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexMapGenerator : MonoBehaviour 
    {
        public HexGrid Grid;

        public void GenerateMap (int x, int z)
        {
            Grid.CreateMap(x, z);
        }
    }
}