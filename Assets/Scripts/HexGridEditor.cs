
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    
    public enum RiverMode { Off, Add, Remove }
    public enum RoadMode { Off, Add, Remove }

    public class HexGridEditor : MonoBehaviour 
    {
        public HexGrid HexGrid;

        int activeTerrain;
        bool applyTerrain = true;

        float activeElevation = 0f;
        bool applyElevation = true;
        public Text ElevationText;

        float activeWaterLevel = 0f;
        bool applyWaterLevel = false;
        public Text WaterLevelText;

        int brushSize = 1;
        public Text BrushSizeText;

        RiverMode riverMode;

        RoadMode roadMode;

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
            if(applyTerrain)
                cell.TerrainTypeIndex = activeTerrain;
            if(applyElevation)
                cell.Elevation = (int)activeElevation;
            if(applyWaterLevel)
                cell.WaterLevel = (int)activeWaterLevel;
            if(riverMode == RiverMode.Remove)
                cell.RemoveRiver();
            if(isDrag && riverMode == RiverMode.Add)
                previousCell.SetOutgoingRiver(dragDirection);
            if(roadMode == RoadMode.Remove)
                cell.RemoveRoad();
            if(isDrag && roadMode == RoadMode.Add)
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

        public void ApplyTerrain(bool disable) => applyTerrain = !disable;

        public void SelectTerrain(int index) => activeTerrain = index;

        public void ApplyElevation(bool disable) => applyElevation = !disable;

        public void SelectElevation(float amount)
        {
            activeElevation = amount;
            ElevationText.text = amount.ToString();
        }

        public void ApplyWaterLevel(bool disable) => applyWaterLevel = !disable;

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

        public void SelectRiverMode(int mode) => riverMode = (RiverMode)mode;

        public void SelectRoadMode(int mode) => roadMode = (RoadMode)mode;

        public void ShowUI(bool visible) => HexGrid.ShowUI(visible);
    }
}