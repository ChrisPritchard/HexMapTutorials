
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexMapCamera : MonoBehaviour 
    {
        Transform swivel, stick;

        float zoom = 1f;

        public HexGrid HexGrid;

        public float StickMinZoom, StickMaxZoom;
        public float MoveSpeedMinZoom, MoveSpeedMaxZoom;

        private void Awake() 
        {
            swivel = transform.GetChild(0);
            stick = swivel.transform.GetChild(0);
        }

        private void Update() 
        {
            var zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (zoomDelta != 0f) 
                AdjustZoom(zoomDelta);

            var xDelta = Input.GetAxis("Horizontal");
            var zDelta = Input.GetAxis("Vertical");
            if (xDelta != 0f || zDelta != 0f)
                AdjustPosition(xDelta, zDelta);
        }

        private void AdjustZoom(float zoomDelta)
        {
            zoom = Mathf.Clamp01(zoom + zoomDelta);
            var distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, distance);
        }

        private void AdjustPosition(float xDelta, float zDelta)
        {
            var direction = new Vector3(xDelta, 0f, zDelta).normalized;
            var damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
            var distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, zoom) * Time.deltaTime;

            var position = transform.localPosition;
            position += direction * damping * distance;
            transform.localPosition = ClampPosition(position);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            var XMax = (HexGrid.Width - 0.5f) * 2f * HexMetrics.InnerRadius;
            position.x = Mathf.Clamp(position.x, 0f, XMax);

            var ZMax = (HexGrid.Height - 1f) * 1.5f * HexMetrics.OuterRadius;
            position.z = Mathf.Clamp(position.z, 0f, ZMax);

            return position;
        }
    }
}