
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    
    public enum BrushMode { Terrain, Elevation, WaterLevel, Rivers, Roads }

    public class HexGridEditor : MonoBehaviour 
    {
        public HexGrid HexGrid;

        public BrushMode Mode;

        int activeTerrain;

        float activeElevation = 0f;
        public Text ElevationText;

        float activeWaterLevel = 0f;
        public Text WaterLevelText;

        bool addRivers = true;

        bool addRoads = true;
        
        int brushSize = 1;
        public Text BrushSizeText;

        new Camera camera;
        EventSystem eventSystem;

        bool isDrag;
        HexDirection dragDirection;
        HexCell previousCell;
        HexCell prevPreviousCell;

        private void Awake() 
        {
            camera = Camera.main;
            eventSystem = EventSystem.current;
            activeTerrain = 0;
        }

        private void Update() 
        {
            if(Input.GetMouseButton(0) && !eventSystem.IsPointerOverGameObject())
                HandleInput();
            else
                previousCell = null;
        }

        private void HandleInput()
        {
            var inputRay = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hit))
            {
                var target = HexGrid.GetCell(hit.point);
                EditCells(target);
                prevPreviousCell = previousCell;
                previousCell = target;
            }
            else
                previousCell = null;
        }

        private void EditCells(HexCell center)
        {
            // prevPrevious cell prevents quick double backs - i *think* this works
            if(previousCell != null && previousCell != center && prevPreviousCell != null && prevPreviousCell != center)
                isDrag = ValidateDrag(center);
            else
                isDrag = false;

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

            if (Mode == BrushMode.Terrain)
                cell.TerrainTypeIndex = activeTerrain;
            if(Mode == BrushMode.Elevation)
                cell.Elevation = (int)activeElevation;
            if(Mode == BrushMode.WaterLevel)
                cell.WaterLevel = (int)activeWaterLevel;
            if(Mode == BrushMode.Rivers && !addRivers)
                cell.RemoveRiver();
            if(Mode == BrushMode.Rivers && addRivers && isDrag)
                previousCell.SetOutgoingRiver(dragDirection);
            if(Mode == BrushMode.Roads && !addRoads)
                cell.RemoveRoad();
            if(Mode == BrushMode.Roads && addRoads && isDrag)
                previousCell.AddRoad(dragDirection);       
        }

        // test if cell is neighbour of previous cell
        private bool ValidateDrag(HexCell newCell)
        {
            for(dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
                if(previousCell.GetNeighbour(dragDirection) == newCell)
                    return true;
            return false;
        }

        public void SelectBrushMode(int mode)
        { 
            Mode = (BrushMode)mode;
            Debug.Log("Mode is now " + Mode);
        }

        public void SelectTerrain(int index) => activeTerrain = index;

        public void SelectElevation(float amount)
        {
            activeElevation = amount;
            ElevationText.text = amount.ToString();
        }

        public void SelectWaterLevel(float amount)
        {
            activeWaterLevel = amount;
            WaterLevelText.text = amount.ToString();
        }

        public void SelectBrushSize(float amount)
        {
            brushSize = (int)amount;
            BrushSizeText.text = brushSize.ToString();
        }

        public void AddRivers(bool value) => addRivers = value;

        public void AddRoads(bool value) => addRoads = value;

        public void ShowUI(bool visible) => HexGrid.ShowUI(visible);
    }
}