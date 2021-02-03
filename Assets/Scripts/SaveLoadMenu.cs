
namespace DarkDomains
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class SaveLoadMenu : MonoBehaviour 
    {
        const int version = 0;
        
        public HexGrid HexGrid;
        public HexMapCamera HexMapCamera;

        public Text Heading;
        public Text ActionButton;
        public ScrollRect FolderView;
        public InputField FileName;

        public SaveLoadFileItem ItemPrefab;

        private Action onAction;

        public void Show(bool saveMode)
        {
            HexMapCamera.Locked = true;
            gameObject.SetActive(true);

            ResetView();

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

        public void Select(string file) => FileName.text = file;

        private void ResetView()
        {
            FileName.text = "";
            var files = FolderView.content.GetComponentsInChildren<SaveLoadFileItem>();
            foreach(var file in files)
                Destroy(file.gameObject);

            var existing = Directory.GetFiles(Application.persistentDataPath, "*.map");
            Array.Sort(existing);
            foreach(var file in existing)
            {
                var instance = Instantiate(ItemPrefab);
                instance.Label.text = Path.GetFileNameWithoutExtension(file);
                instance.Menu = this;
                instance.transform.SetParent(FolderView.content);
            }
        }

        public void Delete()
        {
            if(FileName.text.Trim().Length == 0)
                return;
            var path = Path.Combine(Application.persistentDataPath, FileName.text + ".map");

            if(!File.Exists(path))
            {
                Debug.Log("File " + FileName.text + " does not exist");
                return;
            }
            
            // todo confirm

            File.Delete(path);
            ResetView();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            HexMapCamera.Locked = false;
        }

        private void SaveMap()
        {
            if(FileName.text.Trim().Length == 0)
                return;
            var path = Path.Combine(Application.persistentDataPath, FileName.text + ".map");

            // todo confirm overwrite

            using (var file = File.Open(path, FileMode.Create))
            using (var writer = new BinaryWriter(file))
            { 
                writer.Write(version);
                HexGrid.Save(writer); 
            }
            
            Hide();
        }

        private void LoadMap()
        {
            if(FileName.text.Trim().Length == 0)
                return;
            var path = Path.Combine(Application.persistentDataPath, FileName.text + ".map");

            if(!File.Exists(path))
            {
                Debug.Log("File " + FileName.text + " does not exist");
                return;
            }

            using (var file = File.OpenRead(path))
            using (var reader = new BinaryReader(file))
            { 
                var fileVersion = reader.ReadInt32();
                if(fileVersion != version)
                    Debug.Log("invalid version in save file: " + fileVersion);
                else
                    HexGrid.Load(reader);
            }
            
            HexMapCamera.ValidatePosition();
            Hide();
        }
    }
}