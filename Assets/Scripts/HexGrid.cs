
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;
    
    public class HexGrid : MonoBehaviour 
    {
        public int Width = 6, Height = 6;

        public HexCell CellPrefab;
        public Text CellLabelPrefab;

        HexCell[] cells;
        Canvas canvas;

        private void Awake() 
        {
            cells = new HexCell[Width * Height];
            canvas = GetComponentInChildren<Canvas>();

            for(var x = 0; x < Width; x++)
                for(var z = 0; z < Height; z++)
                    cells[z*Width+x] = CreateCell(x, z);
        }

        private HexCell CreateCell(int x, int z)
        {
            var position = new Vector3(x * HexMetrics.OuterRadius, 0f, z * HexMetrics.OuterRadius);

            var cell = Instantiate<HexCell>(CellPrefab);
            cell.transform.SetParent(this.transform);
            cell.transform.localPosition = position;

            var label = Instantiate<Text>(CellLabelPrefab);
            label.rectTransform.SetParent(canvas.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = x + "\n" + z;

            return cell;
        }
    }
}