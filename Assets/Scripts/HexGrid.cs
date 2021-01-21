
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;
    
    public class HexGrid : MonoBehaviour 
    {
        public int Width = 6, Height = 6;

        public HexCell CellPrefab;
        public Text CellLabelPrefab;

        new Camera camera;
        Canvas canvas;
        HexMesh hexMesh;

        HexCell[] cells;

        private void Awake() 
        {
            camera = Camera.main;
            canvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();

            cells = new HexCell[Width * Height];
            for(var x = 0; x < Width; x++)
                for(var z = 0; z < Height; z++)
                    cells[z*Width+x] = CreateCell(x, z);
        }

        private HexCell CreateCell(int x, int z)
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

            return cell;
        }

        private void Start() 
        {
            hexMesh.Triangulate(cells);
        }

        private void Update() 
        {
            if(Input.GetMouseButtonUp(0))
                HandleInput();
        }

        private void HandleInput()
        {
            var inputRay = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hit))
                TouchCell(hit.point);
        }

        private void TouchCell(Vector3 position)
        {
            position = transform.InverseTransformPoint(position);
            Debug.Log("touched at " + position + "\nhex: " + HexCoordinates.FromPosition(position));
        }
    }
}