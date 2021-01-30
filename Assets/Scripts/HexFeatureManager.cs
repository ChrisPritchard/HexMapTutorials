
namespace DarkDomains
{
    using UnityEngine;
    
    public class HexFeatureManager : MonoBehaviour 
    {
        public Transform FeaturePrefab;

        Transform container;

        public void Clear() 
        { 
            if(container)
                Destroy(container.gameObject);
            container = new GameObject("Features Container").transform;
            container.SetParent(this.transform, false);
        }

        public void Apply() { }

        public void AddFeature (HexCell cell, Vector3 position) 
        { 
            var hash = HexMetrics.SampleHashGrid(position);
            if(hash.A >= cell.UrbanLevel * 0.25f)
                return;

            var instance = Instantiate(FeaturePrefab);
            position.y += instance.localScale.y /2;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.B, 0f);
            instance.SetParent(container, false);
        }
    }
}