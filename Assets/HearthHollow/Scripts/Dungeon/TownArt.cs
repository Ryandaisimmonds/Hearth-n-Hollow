// Hearth & Hollow — the art used by Hearth's Hollow (the walkable town): Cainos "Pixel Art Top Down - Basic".
// Built by "Hearth & Hollow > Apply Cainos Art" (Editor/CainosArtBuilder.cs). Props are looked up by prefab name.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    [CreateAssetMenu(menuName = "Hearth & Hollow/Town Art", fileName = "TownArt")]
    public class TownArt : ScriptableObject
    {
        [Header("Ground")]
        public TileBase grass;
        public TileBase grassFlowers;
        [Range(0f, 1f)] public float flowerChance = 0.12f;
        public TileBase path;        // cobbles in the grass: lanes, the south road, the market street
        public TileBase square;      // big stone slabs: the town square
        public TileBase wallTop;     // the raised ground around the town (solid)
        public TileBase wallFace;    // stone face where the raised ground meets the open ground (solid)
        public List<TileBase> decor = new List<TileBase>();
        [Range(0f, 0.3f)] public float decorDensity = 0.06f;

        [Header("Props (Cainos prefabs)")]
        public List<GameObject> props = new List<GameObject>();

        public GameObject Prop(string name)
        {
            foreach (var p in props) if (p != null && (p.name == name || p.name.EndsWith(" - " + name))) return p;
            return null;
        }
    }
}
