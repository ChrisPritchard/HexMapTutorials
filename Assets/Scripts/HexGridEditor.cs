
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    
    public class HexGridEditor : MonoBehaviour 
    {
        public Color[] Colours;
        public HexGrid HexGrid;

        Color activeColour;

        float activeElevation;

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
                EditCell(HexGrid.GetCell(hit.point));
        }

        private void EditCell(HexCell cell)
        {
            cell.Colour = activeColour;
            cell.Elevation = (int)activeElevation;
        }

        public void SelectColour(int index) => activeColour = Colours[index];

        public void SelectElevation(float amount) => activeElevation = amount;
    }
}