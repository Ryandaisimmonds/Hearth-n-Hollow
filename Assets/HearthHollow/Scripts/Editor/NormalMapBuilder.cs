// Hearth & Hollow — "Hearth & Hollow > Generate Normal Maps" (light plan step 3).
// Makes a normal map for every sprite sheet we light (Cainos tiles/props/plants, the monster + character sheets) and
// hooks it up as the sheet's _NormalMap secondary texture, so URP 2D lights wrap round bricks, rocks and armour.
// Height = a bevel from the sprite's edges (so shapes look rounded) + brightness (dark cracks sit lower).
// Normals go to Assets/HearthHollow/Art/Generated/Normals; the originals only get their import settings touched.
// Safe to run again (overwrites the generated maps).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HearthHollow.EditorTools
{
    public static class NormalMapBuilder
    {
        const string Out = "Assets/HearthHollow/Art/Generated/Normals";
        static readonly string[] Folders = { "Assets/Cainos/Pixel Art Top Down - Basic/Texture", "Assets/HearthHollow/Art/ThirdParty/Top Down Monsters Vol 1" };

        [MenuItem("Hearth & Hollow/Generate Normal Maps", priority = 6)]
        public static void All()
        {
            var list = Sources();
            for (int i = 0; i < list.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Normal maps", list[i], i / (float)list.Count);
                try { Make(list[i]); } catch (System.Exception e) { Debug.LogWarning("[Hearth & Hollow] Normal map failed for " + list[i] + ": " + e.Message); }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearth & Hollow] Normal maps made for " + list.Count + " sprite sheets.");
        }

        public static List<string> Sources()
        {
            var list = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", Folders))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var name = Path.GetFileNameWithoutExtension(p);
                if (name.Contains("Shadow") || name == "TX Player") continue;   // shadows stay flat; the sample player isn't used
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp == null || imp.textureType != TextureImporterType.Sprite) continue;
                list.Add(p);
            }
            list.Sort();
            return list;
        }

        /// Builds and assigns the normal map for one sheet.
        public static void Make(string path)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(path));
            int w = src.width, h = src.height;
            var px = src.GetPixels32();
            bool tiles = path.Contains("Tileset");
            float strength = tiles ? 1.6f : 2.6f;
            const int Bevel = 3;

            // distance (in pixels, capped) from each opaque pixel to the nearest transparent one
            var dist = new int[w * h];
            var q = new Queue<int>();
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a < 128) { dist[i] = 0; q.Enqueue(i); } else dist[i] = Bevel;
            }
            while (q.Count > 0)
            {
                int i = q.Dequeue(); int x = i % w, y = i / w; int nd = dist[i] + 1;
                if (nd >= Bevel) continue;
                if (x > 0 && dist[i - 1] > nd) { dist[i - 1] = nd; q.Enqueue(i - 1); }
                if (x < w - 1 && dist[i + 1] > nd) { dist[i + 1] = nd; q.Enqueue(i + 1); }
                if (y > 0 && dist[i - w] > nd) { dist[i - w] = nd; q.Enqueue(i - w); }
                if (y < h - 1 && dist[i + w] > nd) { dist[i + w] = nd; q.Enqueue(i + w); }
            }
            var height = new float[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a < 128) { height[i] = 0f; continue; }
                float lum = (0.299f * px[i].r + 0.587f * px[i].g + 0.114f * px[i].b) / 255f;
                float bevel = tiles ? 1f : Mathf.Sqrt(dist[i] / (float)Bevel);
                height[i] = tiles ? lum : bevel * 0.65f + lum * 0.35f;
            }
            var outPx = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (px[i].a < 8) { outPx[i] = new Color32(128, 128, 255, 0); continue; }
                    float l = height[y * w + Mathf.Max(0, x - 1)], r = height[y * w + Mathf.Min(w - 1, x + 1)];
                    float d = height[Mathf.Max(0, y - 1) * w + x], u = height[Mathf.Min(h - 1, y + 1) * w + x];
                    var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    outPx[i] = new Color32((byte)(n.x * 127.5f + 127.5f), (byte)(n.y * 127.5f + 127.5f), (byte)(n.z * 127.5f + 127.5f), px[i].a);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(outPx); tex.Apply();
            Directory.CreateDirectory(Out);
            string baseName = Path.GetFileNameWithoutExtension(path);
            if (path.Replace('\\', '/').Contains("/Ninja Adventure"))   // Ninja Adventure: every character's sheets are called Idle / Walk / Attack, so name them by character
            { string d = Path.GetDirectoryName(path); if (Path.GetFileName(d) == "SeparateAnim") d = Path.GetDirectoryName(d); baseName = "Ninja_" + Path.GetFileName(d) + "_" + baseName; }
            string outPath = Out + "/" + baseName + "_N.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(src);
            AssetDatabase.ImportAsset(outPath);
            var nimp = (TextureImporter)AssetImporter.GetAtPath(outPath);
            nimp.textureType = TextureImporterType.Default; nimp.sRGBTexture = false; nimp.mipmapEnabled = false;
            nimp.filterMode = FilterMode.Point; nimp.textureCompression = TextureImporterCompression.Uncompressed;
            nimp.npotScale = TextureImporterNPOTScale.None; nimp.alphaIsTransparency = false;
            nimp.SaveAndReimport();
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);

            // hook it up as the sheet's _NormalMap
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            var so = new SerializedObject(imp);
            var sec = so.FindProperty("m_SpriteSheet.m_SecondaryTextures");
            int slot = -1;
            for (int i = 0; i < sec.arraySize; i++) if (sec.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "_NormalMap") slot = i;
            if (slot < 0) { sec.arraySize++; slot = sec.arraySize - 1; }
            var e = sec.GetArrayElementAtIndex(slot);
            e.FindPropertyRelative("name").stringValue = "_NormalMap";
            e.FindPropertyRelative("texture").objectReferenceValue = normal;
            so.ApplyModifiedPropertiesWithoutUndo();
            imp.SaveAndReimport();
        }
    }
}
