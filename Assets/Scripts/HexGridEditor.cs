
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    
    public class HexGridEditor : MonoBehaviour 
    {
        public Color[] Colours;
        public HexGrid HexGrid;

        Color activeColour;
        bool applyColour = true;

        float activeElevation = 0f;
        bool applyElevation = true;
        public Text ElevationText;

        int brushSize = 1;
        public Text BrushSizeText;

        new Camera camera;
        EventSystem eventSystem;

        private void Awake() 
        {
            camera = Camera.main;
            eventSystem = EventSystem.current;
            activeColour = Colours[0];
        }

        private void Update() 
        {
            if(Input.GetMouseButtonUp(0) && !eventSystem.IsPointerOverGameObject())
                HandleInput();
        }

        private void HandleInput()
        {
            var inputRay = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hit))
                EditCells(HexGrid.GetCell(hit.point));
        }

        private void EditCells(HexCell center)
        {
            EditCell(center);
            if(brushSize == 1)
                return;

            var c = center.Coordinates;
            var b = brushSize - 1; // when converted to radius, ignore centre

            for (int r = 0, z = c.Z - b; z <= c.Z; z++, r++)
                for (var x = c.X - r; x <= c.X + b; x++)
                    EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            for (int r = 0, z = c.Z + b; z > c.Z; z--, r++)
                for (var x = c.X - b; x <= c.X + r; x++)
                    EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
        }

        private void EditCell(HexCell cell)
        {
            if(!cell)
                return;
            if(applyColour)
                cell.Colour = activeColour;
            if(applyElevation)
                cell.Elevation = (int)activeElevation;
        }

        public void ApplyColour(bool disable) => applyColour = !disable;

        public void SelectColour(int index) => activeColour = Colours[index];

        public void ApplyElevation(bool disable) => applyElevation = !disable;

        public void SelectElevation(float amount)
        {
            activeElevation = amount;
            ElevationText.text = amount.ToString();
        }

        public void SelectBrushSize(float amount)
        {
            brushSize = (int)amount;
            BrushSizeText.text = brushSize.ToString();
        }

        public void ShowUI(bool visible) => HexGrid.ShowUI(visible);
    }
}