
namespace DarkDomains
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class SaveLoadMenu : MonoBehaviour 
    {
        const int version = 1;
        
        public HexGrid HexGrid;
        public HexMapCamera HexMapCamera;

        public Text Heading;
        public Text ActionButton;
        public ScrollRect FolderView;

        private Action onAction;

        public void Show(bool saveMode)
        {
            HexMapCamera.Locked = true;
            gameObject.SetActive(true);

            if(saveMode)
            {
                Heading.text = "Save Map";
                ActionButton.text = "Save";
                onAction = SaveMap;
            }
            else
            {
                Heading.text = "Load Map";
                ActionButton.text = "Load";
                onAction = LoadMap;
            }
        }

        public void OnAction() => onAction();

        public void Delete()
        {

        }

        public void Hide()
        {
            gameObject.SetActive(false);
            HexMapCamera.Locked = false;
        }

        private string SavePath() => Path.Combine(Application.persistentDataPath, "test.map");

        private void SaveMap()
        {
            using (var file = File.Open(SavePath(), FileMode.Create))
            using (var writer = new BinaryWriter(file))
            { 
                writer.Write(version);
                HexGrid.Save(writer); 
            }
            
            Hide();
        }

        private void LoadMap()
        {
            using (var file = File.OpenRead(SavePath()))
            using (var reader = new BinaryReader(file))
            { 
                var fileVersion = reader.ReadInt32();
                if(fileVersion != version)
                    Debug.Log("invalid version in save file: " + fileVersion);
                else
                {
                    HexGrid.Load(reader); 
                    Debug.Log("Loaded from " + SavePath());
                }
            }
            
            HexMapCamera.ValidatePosition();
            Hide();
        }
    }
}