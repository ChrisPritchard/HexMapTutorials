
namespace DarkDomains
{
    using UnityEngine;
    
    public class NewGameMenu : MonoBehaviour 
    {
        public HexGrid HexGrid;
        public HexMapCamera HexMapCamera;
        public HexMapGenerator Generator;

        public void Show()
        {
            HexMapCamera.Locked = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            HexMapCamera.Locked = false;
        }

        private bool generateMap = true;

        public void ToggleMapGeneration (bool toggle) => generateMap = toggle;

        private void CreateMap(int xChunks, int zChunks)
        {
            HexGrid.CreateMap(xChunks, zChunks);
            HexMapCamera.ValidatePosition();
            Hide();
        }

        public void CreateSmallMap()
        {
            if(generateMap)
                Generator.GenerateMap(4, 3);
            else
                CreateMap(4, 3);
        }

        public void CreateMediumMap()
        {
            if(generateMap)
                Generator.GenerateMap(8, 6);
            else
                CreateMap(8, 6);
        }

        public void CreateLargeMap()
        {
            if(generateMap)
                Generator.GenerateMap(16, 12);
            else
                CreateMap(16, 12);
        }
    }
}