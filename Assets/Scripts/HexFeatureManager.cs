
namespace DarkDomains
{
    using System;
    using UnityEngine;
    
    [Serializable]
    public class HexFeatureCollection
    {
        public Transform[] Prefabs;

        // choice is 0 <= N < 1
        public Transform Pick(float choice) => Prefabs[(int)(choice * Prefabs.Length)];
    }

    public class HexFeatureManager : MonoBehaviour 
    {
        public HexFeatureCollection[] UrbanPrefabs;

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

            var instance = Instantiate(PickPrefab(cell.UrbanLevel, hash.A, hash.B));
            position.y += instance.localScale.y /2;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.C, 0f);
            instance.SetParent(container, false);
        }

        private Transform PickPrefab(int level, float hash, float choice)
        {
            if(level > 0)
            {
                var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
                for (var i = 0; i < thresholds.Length; i++)
                    if(hash < thresholds[i])
                        return UrbanPrefabs[i].Pick(choice);
            }
            return null;
        }
    }
}