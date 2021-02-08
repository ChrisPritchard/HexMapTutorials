
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexCellShaderData : MonoBehaviour 
    {
        Texture2D cellTexture;

        public void Initialise(int x, int z)
        {
            cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
            cellTexture.filterMode = FilterMode.Point;
            cellTexture.wrapMode = TextureWrapMode.Clamp;
        }
    }
}