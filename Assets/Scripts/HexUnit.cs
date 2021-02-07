
namespace DarkDomains
{
    using System;
    using System.IO;
    using UnityEngine;
    
    public class HexUnit : MonoBehaviour 
    {
        public static HexUnit UnitPrefab;

        HexCell location;
        public HexCell Location
        {
            get => location;
            set
            {
                if (location == value)
                    return;
                location = value;
                value.Unit = this;
                transform.localPosition = value.Position;
            }
        }

        float orientation;
        public float Orientation
        {
            get => orientation;
            set
            {
                if (orientation == value)
                    return;
                orientation = value;
                transform.localRotation = Quaternion.Euler(0f, value, 0f);
            }
        }

        public void ValidatePosition() => transform.localPosition = location.Position;

        public void Die()
        {
            location.Unit = null;
            Destroy(gameObject);
        }

        public void Save(BinaryWriter writer)
        {
            location.Coordinates.Save(writer);
            writer.Write(orientation);
        }

        public static void Load(BinaryReader reader, HexGrid grid)
        {
            var coords = HexCoordinates.Load(reader);
            var orientation = reader.ReadSingle();
            grid.AddUnit(Instantiate(UnitPrefab), grid.GetCell(coords), orientation);
        }
    }
}