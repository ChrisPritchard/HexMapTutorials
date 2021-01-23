
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexMapCamera : MonoBehaviour 
    {
        Transform swivel, stick;

        float zoom = 1f;

        public float StickMinZoom, StickMaxZoom;

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
        }

        private void AdjustZoom(float zoomDelta)
        {
            zoom = Mathf.Clamp01(zoom + zoomDelta);

            var distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, distance);
        }
    }
}