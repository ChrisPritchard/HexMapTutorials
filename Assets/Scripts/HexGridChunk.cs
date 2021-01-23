
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexGridChunk : MonoBehaviour 
    {
        HexCell[] cells;
        Canvas canvas;
        HexMesh hexMesh;

        private void Awake() 
        {
            canvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();

            cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        }

        private void Start() 
        {
            hexMesh.Triangulate(cells);
        }

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.transform.SetParent(this.transform, false);
            cell.UIRect.SetParent(canvas.transform, false);
        }
    }
}