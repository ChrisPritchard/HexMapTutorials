
namespace DarkDomains
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class HexGameUI : MonoBehaviour 
    {
        public HexGrid Grid;
        
        Camera mainCamera;
        HexCell currentCell;
        HexUnit selectedUnit;

        public void SetEditMode(bool toggle)
        {
            enabled = !toggle;
            Grid.ShowUI(!toggle);
            Grid.ClearPath();
        }

        private void Awake() 
        {
            mainCamera = Camera.main;    
        }

        private bool UpdateCurrentCell()
        {
            var cell = Grid.GetCell(mainCamera.ScreenPointToRay(Input.mousePosition));
            if(cell != currentCell)
            {
                currentCell = cell;
                return true;
            }
            return false;
        }

        private void DoSelection()
        {
            Grid.ClearPath();
            UpdateCurrentCell();
            if(currentCell)
                selectedUnit = currentCell.Unit;
        }

        private void Update() 
        {
            if(!EventSystem.current.IsPointerOverGameObject())
            {
                if(Input.GetMouseButton(0))
                    DoSelection();
                else if (selectedUnit)
                {
                    if(Input.GetMouseButtonDown(1))
                        DoMove();
                    else
                        DoPathfinding();
                }
            }
        }

        public void DoMove()
        {
            if (Grid.HasPath && selectedUnit)
            {
                selectedUnit.Location = currentCell;
                Grid.ClearPath();
            }
        }

        private void DoPathfinding()
        {
            if(!UpdateCurrentCell())
                return;
            if(currentCell && selectedUnit.IsValidDestination(currentCell))
                Grid.FindPath(selectedUnit.Location, currentCell, 24);
            else
                Grid.ClearPath();
        }
    }
}