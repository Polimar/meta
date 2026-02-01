using System.Collections.Generic;
using UnityEngine;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Taglio preciso di una mesh con un piano: due sole metà con bordo condiviso (merge vertici),
    /// UV e normals preservate per texture corretta.
    /// </summary>
    public static class MeshSlice
    {
        const float Epsilon = 0.00001f;
        const float MergeTolerance = 0.0005f;
        const float SpawnOffset = 0.08f;

        public static void SliceAndSpawn(Transform source, Vector3 planeWorldPos, Vector3 planeWorldNormal,
            Material material, Color tint, float forceMagnitude, float lifetime)
        {
            var mf = source.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return;
            var tr = mf.transform;

            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var uvs = mesh.uv;
            var tris = mesh.triangles;
            bool hasUv = uvs != null && uvs.Length == verts.Length;

            Vector3 planeN = planeWorldNormal.normalized;
            Vector3 planeP = planeWorldPos;

            var vertsA = new List<Vector3>();
            var vertsB = new List<Vector3>();
            var uvsA = new List<Vector2>();
            var uvsB = new List<Vector2>();
            var triA = new List<int>();
            var triB = new List<int>();

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i0 = tris[i], i1 = tris[i + 1], i2 = tris[i + 2];
                Vector3 w0 = tr.TransformPoint(verts[i0]);
                Vector3 w1 = tr.TransformPoint(verts[i1]);
                Vector3 w2 = tr.TransformPoint(verts[i2]);
                Vector2 uv0 = hasUv ? uvs[i0] : Vector2.zero;
                Vector2 uv1 = hasUv ? uvs[i1] : Vector2.zero;
                Vector2 uv2 = hasUv ? uvs[i2] : Vector2.zero;

                float d0 = SignedDist(w0, planeP, planeN);
                float d1 = SignedDist(w1, planeP, planeN);
                float d2 = SignedDist(w2, planeP, planeN);

                ClipTriangle(vertsA, uvsA, triA, vertsB, uvsB, triB,
                    w0, w1, w2, uv0, uv1, uv2, d0, d1, d2, planeP, planeN, hasUv);
            }

            if (triA.Count >= 3)
            {
                MergeVertices(vertsA, uvsA, triA);
                AddCapToHalf(vertsA, uvsA, triA, planeP, planeN, 1);
                if (vertsA.Count >= 3 && triA.Count >= 3)
                    SpawnHalf(source.gameObject, mf, material, vertsA, uvsA, triA, planeP, planeN, tint, forceMagnitude, lifetime, 1);
            }
            if (triB.Count >= 3)
            {
                MergeVertices(vertsB, uvsB, triB);
                AddCapToHalf(vertsB, uvsB, triB, planeP, planeN, -1);
                if (vertsB.Count >= 3 && triB.Count >= 3)
                    SpawnHalf(source.gameObject, mf, material, vertsB, uvsB, triB, planeP, planeN, tint, forceMagnitude, lifetime, -1);
            }
        }

        /// <summary>
        /// Chiude il bordo aperto della metà aggiungendo una faccia (cap) sul piano di taglio,
        /// così non si vede l'interno della mesh.
        /// </summary>
        static void AddCapToHalf(List<Vector3> worldVerts, List<Vector2> uvs, List<int> tris,
            Vector3 planeP, Vector3 planeN, int side)
        {
            const float capEpsilon = 0.005f;
            var onPlane = new List<int>();
            for (int i = 0; i < worldVerts.Count; i++)
            {
                float d = SignedDist(worldVerts[i], planeP, planeN);
                if (Mathf.Abs(d) <= capEpsilon)
                    onPlane.Add(i);
            }
            if (onPlane.Count < 3) return;

            Vector3 centroid = Vector3.zero;
            Vector2 uvSum = Vector2.zero;
            foreach (int i in onPlane)
            {
                centroid += worldVerts[i];
                if (uvs != null && i < uvs.Count) uvSum += uvs[i];
            }
            centroid /= onPlane.Count;
            uvSum /= onPlane.Count;

            Vector3 u = (Mathf.Abs(planeN.y) < 0.9f) ? Vector3.Cross(Vector3.up, planeN).normalized : Vector3.Cross(Vector3.right, planeN).normalized;
            Vector3 v = Vector3.Cross(planeN, u);

            onPlane.Sort((a, b) =>
            {
                Vector3 da = worldVerts[a] - centroid;
                Vector3 db = worldVerts[b] - centroid;
                float angA = Mathf.Atan2(Vector3.Dot(da, v), Vector3.Dot(da, u));
                float angB = Mathf.Atan2(Vector3.Dot(db, v), Vector3.Dot(db, u));
                return angA.CompareTo(angB);
            });

            Vector3 edge = worldVerts[onPlane[1]] - worldVerts[onPlane[0]];
            Vector3 toC = centroid - worldVerts[onPlane[0]];
            Vector3 capNormal = Vector3.Cross(edge, toC).normalized;
            bool wantReverse = (side > 0 && Vector3.Dot(capNormal, planeN) < 0) || (side < 0 && Vector3.Dot(capNormal, planeN) > 0);
            if (wantReverse)
                onPlane.Reverse();

            int capIdx = worldVerts.Count;
            worldVerts.Add(centroid);
            if (uvs != null && uvs.Count == worldVerts.Count - 1)
                uvs.Add(uvSum);

            int n = onPlane.Count;
            for (int i = 0; i < n; i++)
            {
                int i0 = capIdx;
                int i1 = onPlane[i];
                int i2 = onPlane[(i + 1) % n];
                if (side > 0)
                { tris.Add(i0); tris.Add(i1); tris.Add(i2); }
                else
                { tris.Add(i0); tris.Add(i2); tris.Add(i1); }
            }
            // Doppia faccia: stesso cap con winding invertito così non si vede mai l'interno (no back-face cull)
            for (int i = 0; i < n; i++)
            {
                int i0 = capIdx;
                int i1 = onPlane[(i + 1) % n];
                int i2 = onPlane[i];
                if (side > 0)
                { tris.Add(i0); tris.Add(i1); tris.Add(i2); }
                else
                { tris.Add(i0); tris.Add(i2); tris.Add(i1); }
            }
        }

        static float SignedDist(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            return Vector3.Dot(point - planePoint, planeNormal);
        }

        static void IntersectEdge(Vector3 a, Vector3 b, Vector2 uva, Vector2 uvb, float da, float db,
            Vector3 planePoint, Vector3 planeNormal, out Vector3 outPoint, out Vector2 outUv)
        {
            float denom = da - db;
            float t = (Mathf.Abs(denom) < Epsilon) ? 0f : Mathf.Clamp01(da / denom);
            outPoint = Vector3.Lerp(a, b, t);
            outUv = Vector2.Lerp(uva, uvb, t);
        }

        static void ClipTriangle(
            List<Vector3> va, List<Vector2> uvA, List<int> ta,
            List<Vector3> vb, List<Vector2> uvB, List<int> tb,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2,
            float d0, float d1, float d2, Vector3 planeP, Vector3 planeN, bool hasUv)
        {
            if (d0 >= -Epsilon && d1 >= -Epsilon && d2 >= -Epsilon)
            {
                int b = va.Count;
                va.Add(v0); va.Add(v1); va.Add(v2);
                if (hasUv) { uvA.Add(uv0); uvA.Add(uv1); uvA.Add(uv2); }
                ta.Add(b); ta.Add(b + 1); ta.Add(b + 2);
                return;
            }
            if (d0 <= Epsilon && d1 <= Epsilon && d2 <= Epsilon)
            {
                int b = vb.Count;
                vb.Add(v0); vb.Add(v1); vb.Add(v2);
                if (hasUv) { uvB.Add(uv0); uvB.Add(uv1); uvB.Add(uv2); }
                tb.Add(b); tb.Add(b + 1); tb.Add(b + 2);
                return;
            }

            Vector3 i01, i12, i20;
            Vector2 i01uv, i12uv, i20uv;
            IntersectEdge(v0, v1, uv0, uv1, d0, d1, planeP, planeN, out i01, out i01uv);
            IntersectEdge(v1, v2, uv1, uv2, d1, d2, planeP, planeN, out i12, out i12uv);
            IntersectEdge(v2, v0, uv2, uv0, d2, d0, planeP, planeN, out i20, out i20uv);

            if (d0 >= -Epsilon && d1 <= Epsilon && d2 <= Epsilon)
            {
                int bA = va.Count;
                va.Add(v0); va.Add(i01); va.Add(i20);
                if (hasUv) { uvA.Add(uv0); uvA.Add(i01uv); uvA.Add(i20uv); }
                ta.Add(bA); ta.Add(bA + 1); ta.Add(bA + 2);
                int bB = vb.Count;
                vb.Add(v1); vb.Add(i01); vb.Add(i20); vb.Add(v2);
                if (hasUv) { uvB.Add(uv1); uvB.Add(i01uv); uvB.Add(i20uv); uvB.Add(uv2); }
                tb.Add(bB); tb.Add(bB + 1); tb.Add(bB + 2);
                tb.Add(bB); tb.Add(bB + 2); tb.Add(bB + 3);
            }
            else if (d0 <= Epsilon && d1 >= -Epsilon && d2 <= Epsilon)
            {
                int bA = va.Count;
                va.Add(v1); va.Add(i01); va.Add(i12);
                if (hasUv) { uvA.Add(uv1); uvA.Add(i01uv); uvA.Add(i12uv); }
                ta.Add(bA); ta.Add(bA + 1); ta.Add(bA + 2);
                int bB = vb.Count;
                vb.Add(v0); vb.Add(i20); vb.Add(i01); vb.Add(v2);
                if (hasUv) { uvB.Add(uv0); uvB.Add(i20uv); uvB.Add(i01uv); uvB.Add(uv2); }
                tb.Add(bB); tb.Add(bB + 1); tb.Add(bB + 2);
                tb.Add(bB); tb.Add(bB + 2); tb.Add(bB + 3);
            }
            else if (d0 <= Epsilon && d1 <= Epsilon && d2 >= -Epsilon)
            {
                int bA = va.Count;
                va.Add(v2); va.Add(i12); va.Add(i20);
                if (hasUv) { uvA.Add(uv2); uvA.Add(i12uv); uvA.Add(i20uv); }
                ta.Add(bA); ta.Add(bA + 1); ta.Add(bA + 2);
                int bB = vb.Count;
                vb.Add(v0); vb.Add(i20); vb.Add(i12); vb.Add(v1);
                if (hasUv) { uvB.Add(uv0); uvB.Add(i20uv); uvB.Add(i12uv); uvB.Add(uv1); }
                tb.Add(bB); tb.Add(bB + 1); tb.Add(bB + 2);
                tb.Add(bB); tb.Add(bB + 2); tb.Add(bB + 3);
            }
            else if (d0 >= -Epsilon && d1 >= -Epsilon && d2 <= Epsilon)
            {
                int bA = va.Count;
                va.Add(v0); va.Add(v1); va.Add(i01); va.Add(i20);
                if (hasUv) { uvA.Add(uv0); uvA.Add(uv1); uvA.Add(i01uv); uvA.Add(i20uv); }
                ta.Add(bA); ta.Add(bA + 1); ta.Add(bA + 2);
                ta.Add(bA); ta.Add(bA + 2); ta.Add(bA + 3);
                int bB = vb.Count;
                vb.Add(v2); vb.Add(i12); vb.Add(i20); vb.Add(i01);
                if (hasUv) { uvB.Add(uv2); uvB.Add(i12uv); uvB.Add(i20uv); uvB.Add(i01uv); }
                tb.Add(bB); tb.Add(bB + 1); tb.Add(bB + 2);
                tb.Add(bB); tb.Add(bB + 2); tb.Add(bB + 3);
            }
            else if (d0 >= -Epsilon && d1 <= Epsilon && d2 >= -Epsilon)
            {
                int bA = va.Count;
                va.Add(v0); va.Add(v2); va.Add(i20); va.Add(i01);
                if (hasUv) { uvA.Add(uv0); uvA.Add(uv2); uvA.Add(i20uv); uvA.Add(i01uv); }
                ta.Add(bA); ta.Add(bA + 1); ta.Add(bA + 2);
                ta.Add(bA); ta.Add(bA + 2); ta.Add(bA + 3);
                int bB = vb.Count;
                vb.Add(v1); vb.Add(i01); vb.Add(i12);
                if (hasUv) { uvB.Add(uv1); uvB.Add(i01uv); uvB.Add(i12uv); }
                tb.Add(bB); tb.Add(bB + 1); tb.Add(bB + 2);
            }
            else
            {
                int bA = va.Count;
                va.Add(v1); va.Add(v2); va.Add(i12); va.Add(i01);
                if (hasUv) { uvA.Add(uv1); uvA.Add(uv2); uvA.Add(i12uv); uvA.Add(i01uv); }
                ta.Add(bA); ta.Add(bA + 1); ta.Add(bA + 2);
                ta.Add(bA); ta.Add(bA + 2); ta.Add(bA + 3);
                int bB = vb.Count;
                vb.Add(v0); vb.Add(i20); vb.Add(i01);
                if (hasUv) { uvB.Add(uv0); uvB.Add(i20uv); uvB.Add(i01uv); }
                tb.Add(bB); tb.Add(bB + 1); tb.Add(bB + 2);
            }
        }

        static void MergeVertices(List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            var newVerts = new List<Vector3>();
            var newUvs = new List<Vector2>();
            bool hasUv = uvs != null && uvs.Count == verts.Count;
            var indexMap = new int[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                int found = -1;
                for (int j = 0; j < newVerts.Count; j++)
                {
                    if (Vector3.SqrMagnitude(newVerts[j] - v) < MergeTolerance * MergeTolerance)
                    {
                        found = j;
                        break;
                    }
                }
                if (found >= 0)
                    indexMap[i] = found;
                else
                {
                    indexMap[i] = newVerts.Count;
                    newVerts.Add(v);
                    if (hasUv)
                        newUvs.Add(uvs[i]);
                }
            }
            verts.Clear();
            verts.AddRange(newVerts);
            if (hasUv)
            {
                uvs.Clear();
                uvs.AddRange(newUvs);
            }
            for (int i = 0; i < tris.Count; i++)
                tris[i] = indexMap[tris[i]];
        }

        static void SpawnHalf(GameObject original, MeshFilter sourceMf, Material material, List<Vector3> worldVerts, List<Vector2> uvs, List<int> triangles,
            Vector3 planePoint, Vector3 planeNormal, Color tint, float forceMag, float lifetime, int side)
        {
            // Direzione sinistra/destra rispetto alla spada (nel piano di taglio), non avanti/dietro
            Vector3 sepDir = Vector3.Cross(planeNormal.normalized, Vector3.up).normalized;
            if (sepDir.sqrMagnitude < 0.01f)
                sepDir = Vector3.Cross(planeNormal.normalized, Vector3.forward).normalized;

            var go = new GameObject("SliceHalf");
            if (original.transform.parent != null)
                go.transform.SetParent(original.transform.parent, true);
            Vector3 offset = sepDir * (SpawnOffset * side);
            go.transform.position = original.transform.position + offset;
            go.transform.rotation = original.transform.rotation;
            go.transform.localScale = original.transform.lossyScale;

            var mesh = new Mesh();
            mesh.name = "SliceHalfMesh";
            var localVerts = new Vector3[worldVerts.Count];
            for (int i = 0; i < worldVerts.Count; i++)
                localVerts[i] = go.transform.InverseTransformPoint(worldVerts[i]);
            mesh.SetVertices(localVerts);
            if (uvs != null && uvs.Count == worldVerts.Count)
                mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            var mfNew = go.AddComponent<MeshFilter>();
            mfNew.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            Material matToUse = null;
            if (material != null)
            {
                matToUse = Object.Instantiate(material);
                matToUse.color = tint;
            }
            var rend = sourceMf.GetComponent<MeshRenderer>();
            if (matToUse == null && rend != null && rend.sharedMaterial != null)
            {
                matToUse = Object.Instantiate(rend.sharedMaterial);
                matToUse.color = tint;
            }
            if (matToUse == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                matToUse = shader != null ? new Material(shader) : null;
                if (matToUse != null)
                {
                    matToUse.color = tint;
                    if (matToUse.HasProperty("_Surface"))
                        matToUse.SetFloat("_Surface", 0);
                }
            }
            if (matToUse != null)
            {
                mr.material = matToUse;
                // Doppia faccia così la faccia di taglio non appare invertita / cullata
                if (matToUse.HasProperty("_Cull"))
                    matToUse.SetInt("_Cull", 0);
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;
            float sepForce = forceMag * 2f;
            rb.AddForce(sepDir * (side * sepForce), ForceMode.Impulse);

            // Collider così le due metà si respingono e non si sovrappongono (evita z-fight/geometria frammentata)
            var bc = go.AddComponent<BoxCollider>();
            bc.center = mesh.bounds.center;
            bc.size = mesh.bounds.size;

            Object.Destroy(go, lifetime);
        }
    }
}
