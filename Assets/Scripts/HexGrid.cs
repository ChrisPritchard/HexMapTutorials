
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;
    
    public class HexGrid : MonoBehaviour 
    {
        public int Width = 6, Height = 6;

        public HexCell CellPrefab;
        public Text CellLabelPrefab;

        Canvas canvas;
        HexMesh hexMesh;

        HexCell[] cells;

        private void Awake() 
        {
            canvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();

            cells = new HexCell[Width * Height];
            for(var x = 0; x < Width; x++)
                for(var z = 0; z < Height; z++)
                    cells[z*Width+x] = CreateCell(x, z);
        }

        private HexCell CreateCell(int x, int z)
        {
            var px = x * (2 * HexMetrics.InnerRadius) + (z % 2) * HexMetrics.InnerRadius;
            var pz = z * (1.5f * HexMetrics.OuterRadius);
            var position = new Vector3(px, 0f, pz);

            var cell = Instantiate<HexCell>(CellPrefab);
            cell.transform.SetParent(this.transform);
            cell.transform.localPosition = position;

            var label = Instantiate<Text>(CellLabelPrefab);
            label.rectTransform.SetParent(canvas.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = x + "\n" + z;

            return cell;
        }

        private void Start() 
        {
            hexMesh.Triangulate(cells);
        }
    }
}