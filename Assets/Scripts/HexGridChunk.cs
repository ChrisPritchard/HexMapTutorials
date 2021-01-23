
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

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.transform.SetParent(this.transform, false);
            cell.UIRect.SetParent(canvas.transform, false);
            cell.Chunk = this;
        }

        // chunks are only enabled when they need to triangulate
        public void Refresh()
        {
            enabled = true;
        }

        // this method will only be invoked if the chunk is enabled, and will then disable itself
        private void LateUpdate() 
        {
            hexMesh.Triangulate(cells); 
            enabled = false;
        }
    }
}