
namespace DarkDomains
{
    using System;
    using System.Collections.Generic;
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
        public HexFeatureCollection[] UrbanPrefabs, FarmPrefabs, ForestPrefabs;

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

            var prefab = PickPrefab(cell, hash);
            if(!prefab)
                return;

            var instance = Instantiate(prefab);
            position.y += instance.localScale.y /2;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.E, 0f);
            instance.SetParent(container, false);
        }

        private Transform PickPrefab(HexCell cell, HexHash hash)
        {
            var options = new Dictionary<float, Transform>
            {
                [hash.A] = PickPrefab(UrbanPrefabs, cell.UrbanLevel, hash.A, hash.D),
                [hash.B] = PickPrefab(FarmPrefabs, cell.FarmLevel, hash.B, hash.D),
                [hash.C] = PickPrefab(ForestPrefabs, cell.ForestLevel, hash.C, hash.D)
            };
            
            var odds = new[] {hash.A, hash.B, hash.C};
            Array.Sort(odds);
            foreach(var o in odds)
                if(options[o])
                    return options[o];
            return null;
        }

        private Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
        {
            if(level > 0)
            {
                var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
                for (var i = 0; i < thresholds.Length; i++)
                    if(hash < thresholds[i])
                        return collection[i].Pick(choice);
            }
            return null;
        }
    }
}