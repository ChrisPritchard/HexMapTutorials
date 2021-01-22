
namespace DarkDomains
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    
    public class HexGridEditor : MonoBehaviour 
    {
        public Color[] Colours;
        public HexGrid HexGrid;

        Color activeColour;

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
                HexGrid.TouchCell(hit.point, activeColour);
        }

        public void SelectColour(int index) => activeColour = Colours[index];
    }
}