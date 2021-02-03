
namespace DarkDomains
{
    using UnityEngine;
    
    public class NewGameMenu : MonoBehaviour 
    {
        public HexGrid HexGrid;
        public HexMapCamera HexMapCamera;

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

        private void CreateMap(int xChunks, int zChunks)
        {
            HexGrid.CreateMap(xChunks, zChunks);
            HexMapCamera.ValidatePosition();
            Hide();
        }

        public void CreateSmallMap() => CreateMap(4, 3);

        public void CreateMediumMap() => CreateMap(8, 6);

        public void CreateLargeMap() => CreateMap(16, 12);
    }
}