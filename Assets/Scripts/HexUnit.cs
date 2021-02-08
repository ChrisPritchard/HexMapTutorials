
namespace DarkDomains
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    
    public class HexUnit : MonoBehaviour 
    {
        public HexGrid Grid { get; set; }

        public static HexUnit UnitPrefab;

        List<HexCell> pathToTravel;

        const float travelSpeed = 4f;
        const float rotationSpeed = 180f;
        const int visionRange = 3;

        HexCell location, currentTravelLocation;

        public HexCell Location
        {
            get => location;
            set
            {
                if (location == value)
                    return;
                if (location)
                {
                    Grid.DecreaseVisibility(location, visionRange);
                    location.Unit = null;
                }
                location = value;
                value.Unit = this;
                Grid.IncreaseVisibility(value, visionRange);
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

        private void OnEnable() 
        {
            if(location) 
            {
                transform.localPosition = location.Position;
                if(currentTravelLocation) // catches recompile while moving... probably unnecessary
                {
                    Grid.IncreaseVisibility(location, visionRange);
                    Grid.DecreaseVisibility(currentTravelLocation, visionRange);
                    currentTravelLocation = null;
                }
            }
        }

        public void ValidatePosition() => transform.localPosition = location.Position;

        public void Die()
        {
            location.Unit = null;
            Grid.DecreaseVisibility(location, visionRange);
            Destroy(gameObject);
        }

        public bool IsValidDestination(HexCell cell)
        {
            return !cell.IsUnderwater && !cell.Unit;
        }

        IEnumerator LookAt(Vector3 point)
        {
            point.y = transform.localPosition.y;
            var fromRotation = transform.localRotation;
            var toRotation = Quaternion.LookRotation(point - transform.localPosition);
            var angle = Quaternion.Angle(fromRotation, toRotation);

            if(angle > 0f)
            {
                var speed = rotationSpeed / angle;
                for(var t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
                {
                    transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                    yield return null;
                }
            }

            transform.LookAt(point);
            orientation = transform.localRotation.eulerAngles.y;
        }

        public void Travel(List<HexCell> path)
        {
            location.Unit = null;
            location = path[path.Count - 1];
            location.Unit = this;

            pathToTravel = path;
            StopAllCoroutines();
            StartCoroutine(TravelPath());
        }

        private IEnumerator TravelPath()
        {
            Vector3 a, b, c = pathToTravel[0].Position;
            yield return LookAt(pathToTravel[1].Position);
            Grid.DecreaseVisibility( // catches mid move switch, requiring current location to lose visibility
                currentTravelLocation ? currentTravelLocation : pathToTravel[0], visionRange);

            var t = Time.deltaTime * travelSpeed;
            for(var i = 1; i <= pathToTravel.Count; i++)
            {
                if(i != pathToTravel.Count)
                    currentTravelLocation = pathToTravel[i];
                a = c;
                b = pathToTravel[i - 1].Position;
                c = i == pathToTravel.Count
                    ? b : (b + pathToTravel[i].Position) * 0.5f;
                if (i != pathToTravel.Count)
                    Grid.IncreaseVisibility(pathToTravel[i], visionRange);
                for(; t < 1f; t += Time.deltaTime * travelSpeed)
                {
                    transform.localPosition = Bezier.GetPoint(a, b, c, t);
                    var d = Bezier.GetDirivative(a, b, c, t);
                    d.y = 0f;
                    transform.localRotation = Quaternion.LookRotation(d);
                    yield return null;
                }
                if(i != pathToTravel.Count)
                    Grid.DecreaseVisibility(pathToTravel[i-1], visionRange);
                t -= 1f;
            }
            currentTravelLocation = null;
            transform.localPosition = Location.Position;
            Orientation = transform.localRotation.eulerAngles.y;
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