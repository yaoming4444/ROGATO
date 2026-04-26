using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Companions
{
    [CreateAssetMenu(fileName = "CompanionDatabase", menuName = "GameCore/Companions/Companion Database")]
    public class CompanionDatabase : ScriptableObject
    {
        [SerializeField] private List<CompanionDef> companions = new();

        private Dictionary<string, CompanionDef> byId;

        public IReadOnlyList<CompanionDef> Companions => companions;

        private void OnEnable()
        {
            RebuildIndex();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildIndex();
        }
#endif

        public CompanionDef GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (byId == null)
                RebuildIndex();

            byId.TryGetValue(id, out var def);
            return def;
        }

        public bool Contains(string id)
        {
            return GetById(id) != null;
        }

        private void RebuildIndex()
        {
            byId = new Dictionary<string, CompanionDef>();

            if (companions == null)
                return;

            foreach (var def in companions)
            {
                if (def == null)
                    continue;

                if (string.IsNullOrWhiteSpace(def.id))
                {
                    Debug.LogWarning($"[CompanionDatabase] CompanionDef '{def.name}' has empty id.", def);
                    continue;
                }

                if (byId.ContainsKey(def.id))
                {
                    Debug.LogWarning($"[CompanionDatabase] Duplicate companion id '{def.id}' found.", def);
                    continue;
                }

                byId.Add(def.id, def);
            }
        }
    }
}