
namespace HexMapTutorials
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class SaveLoadFileItem : MonoBehaviour 
    {
        public Text Label;
        public Action OnAction;
        public SaveLoadMenu Menu;

        public void Click() => Menu.Select(Label.text);
    }
}