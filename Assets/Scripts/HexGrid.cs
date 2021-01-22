
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;

    public class HexGrid : MonoBehaviour
    {
        public int Width = 6, Height = 6;
        public Color DefaultColour = Color.white;

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
            for(var z = 0; z < Height; z++)
                for(var x = 0; x < Width; x++)
                    CreateCell(x, z);
        }

        private void CreateCell(int x, int z)
        {
            var px = (x + z/2f - z/2) * (2 * HexMetrics.InnerRadius);
            var pz = z * (1.5f * HexMetrics.OuterRadius);
            var position = new Vector3(px, 0f, pz);

            var cell = Instantiate<HexCell>(CellPrefab);
            cell.transform.SetParent(this.transform);
            cell.transform.localPosition = position;
            cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

            var label = Instantiate<Text>(CellLabelPrefab);
            label.rectTransform.SetParent(canvas.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.Coordinates.ToString("\n");
            cell.UIRect = label.rectTransform;

            var index = z * Width + x;
            
            // TODO - remove random setters. They're here just for ease of testing of new features for now
            cell.Colour = new[]{Color.green, Color.red, Color.blue, Color.yellow, Color.white}[Random.Range(0, 5)];
            cell.Elevation = Random.Range(0, 3);
            cells[index] = cell;

            // connect neighbours, working backwards. e.g. connect the prior, and the bottom two corners if available
            // the setneighbour function does the reverse, so connecting back will conneck the prior cell to the current one too
            // in this way, all cells are connected to their neighbours

            if (x != 0)
                cell.SetNeighbour(HexDirection.W, cells[index - 1]);
            if (z == 0)
                return;
            
            if (z % 2 == 0) // non 'shunted' row, so always has bottom right, but first doesnt have bottom left
            {
                cell.SetNeighbour(HexDirection.SE, cells[index - Width]);
                if (x != 0)
                    cell.SetNeighbour(HexDirection.SW, cells[index - Width - 1]);
            } else  // 'shunted' row, always has bottom left, but last does not have bottom right
            {
                cell.SetNeighbour(HexDirection.SW, cells[index - Width]);
                if (x != Width - 1)
                    cell.SetNeighbour(HexDirection.SE, cells[index - Width + 1]);
            }
        }

        private void Start()
        {
            hexMesh.Triangulate(cells);
        }

        public HexCell GetCell(Vector3 position)
        {
            position = transform.InverseTransformPoint(position);
            var coords = HexCoordinates.FromPosition(position);
            var index = coords.Z*Width+coords.X + coords.Z/2;
            return cells[index];
        }

        public void Refresh()
        {
            hexMesh.Triangulate(cells);
        }
    }
}