using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Encounter-local surface dressing. All damage is bounded, non-blocking and removed with the room.
// Reuses the project's imported plaster/timber materials; does not modify source assets.
public class MedicalDoorwaySurfaceDetail : MonoBehaviour
{
    public bool HeaderSealed { get; private set; }
    public Bounds HeaderBounds { get; private set; }
    public int SurfaceCount { get; private set; }
    public int ImpactCount { get; private set; }
    public int GougeCount { get; private set; }
    public int ShavingCount => shavings.Count;
    public int SettledShavings { get { int n = 0; foreach (var s in shavings) if (s.settled) n++; return n; } }
    public float HighestSettledShaving { get { float y = 0; foreach (var s in shavings) if (s.settled && s.transform) y = Mathf.Max(y, s.transform.position.y - floorY); return y; } }

    const int MaximumImpacts = 8;
    const int ShavingsPerImpact = 18;
    MedicalInterior room;
    Transform detailRoot;
    Renderer frame;
    Material plaster, exposed, timber, cavity, damp;
    Mesh shavingMesh;
    float floorY, centerX, planeZ, frontZ;
    readonly List<Material> ownedMaterials = new List<Material>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    readonly List<Shaving> shavings = new List<Shaving>();
    readonly List<OriginalSurface> originalSurfaces = new List<OriginalSurface>();
    readonly List<Vector3> impacts = new List<Vector3>();
    readonly System.Random random = new System.Random(2384);

    class Shaving { public Transform transform; public Vector3 velocity, spin; public bool settled; public float yaw; }
    struct OriginalSurface { public Renderer renderer; public Material material; }

    public void Configure(MedicalInterior interior)
    {
        if (!interior || !interior.jammedDoor || detailRoot) return;
        room = interior;
        floorY = room.transform.position.y;
        centerX = room.transform.position.x;
        planeZ = room.jammedDoor.position.z;
        var all = room.GetComponentsInChildren<Renderer>(true);
        Material plasterSource = null, timberSource = null;
        Renderer lintel = null;
        foreach (var r in all)
        {
            if (r.name == "Textured research door frame" && Mathf.Abs(r.bounds.center.z - planeZ) < .5f) frame = r;
            if (r.name == "Fixed lintel" && Mathf.Abs(r.bounds.center.z - planeZ) < .5f) lintel = r;
            if (r.sharedMaterial && r.sharedMaterial.name.StartsWith("Peeling plaster")) plasterSource = r.sharedMaterial;
        }
        // Woodland remains loaded when entering the medical building, so its licensed timber is normally available.
        foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
            if (m && m.name == "Weathered timber") { timberSource = m; break; }
        if (!plasterSource) plasterSource = Resources.Load<Material>("Medical/ThornbackPlaster");
        if (!plasterSource) { Debug.LogWarning("Doorway surface detail requires a plaster material."); return; }
        detailRoot = new GameObject("Doorway masonry, contact scars and shavings").transform;
        detailRoot.SetParent(room.transform, false);
        frontZ = frame ? Mathf.Max(frame.bounds.max.z, planeZ + .125f) : planeZ + .22f;
        plaster = Copy(plasterSource, "Fitted doorway plaster", new Color(.64f, .63f, .55f), .16f);
        exposed = Copy(plasterSource, "Exposed rough plaster edges", new Color(.53f, .53f, .47f), .08f);
        damp = Copy(plasterSource, "Damp plaster and hairline fractures", new Color(.20f, .225f, .19f), .24f);
        timber = Copy(timberSource ? timberSource : plasterSource, "Freshly torn timber fibres", new Color(.66f, .44f, .23f), .1f);
        cavity = Copy(plasterSource, "Dark recess below chipped paint", new Color(.075f, .047f, .024f), .03f);
        timber.SetTextureScale("_BaseMap", new Vector2(.25f, 3f));
        if (timber.HasProperty("_BumpScale")) timber.SetFloat("_BumpScale", .65f);
        if (plaster.HasProperty("_BumpScale")) plaster.SetFloat("_BumpScale", 1.15f);

        // Fit a solid infill between the real imported frame top and existing structural header.
        float bottom = frame ? frame.bounds.max.y - .014f : floorY + 2.37f;
        float top = lintel ? lintel.bounds.max.y + .006f : floorY + 2.61f;
        float width = lintel ? lintel.bounds.size.x : 2.55f;
        top = Mathf.Max(top, bottom + .04f);
        var header = Box("Closed plaster lintel infill", new Vector3(centerX, (bottom + top) * .5f, planeZ), new Vector3(width, top - bottom, .25f), plaster);
        HeaderBounds = header.GetComponent<Renderer>().bounds;
        HeaderSealed = !frame || HeaderBounds.min.y <= frame.bounds.max.y;
        SurfaceCount++;
        SurfaceWeathering(all);
        DressDoorwayCheeks();
        shavingMesh = SplinterMesh();
    }

    Material Copy(Material source, string label, Color tint, float smoothness)
    {
        var material = new Material(source) { name = label, enableInstancing = true };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        ownedMaterials.Add(material);
        return material;
    }

    void SurfaceWeathering(Renderer[] renderers)
    {
        foreach (var r in renderers)
        {
            if (!r.sharedMaterial || r.bounds.max.z < planeZ - 4 || r.bounds.min.z > planeZ + 6 || Mathf.Abs(r.bounds.center.x - centerX) > 2.4f) continue;
            if (r.name != "Stained plaster" && r.name != "Doorway side wall" && r.name != "Worn concrete floor") continue;
            var mat = new Material(r.sharedMaterial) { name = r.sharedMaterial.name + " - encounter texel scale", enableInstancing = true };
            ownedMaterials.Add(mat);
            originalSurfaces.Add(new OriginalSurface { renderer = r, material = r.sharedMaterial });
            r.sharedMaterial = mat;
            var b = r.bounds;
            // At least one texture repeat per 1.2m, instead of stretching one scan over the entire corridor wall.
            var scale = r.name == "Worn concrete floor" ? new Vector2(Mathf.Max(1, b.size.x / 1.5f), Mathf.Max(1, b.size.z / 1.5f)) : new Vector2(Mathf.Max(.6f, Mathf.Max(b.size.x, b.size.z) / 1.2f), Mathf.Max(.7f, b.size.y / 1.2f));
            mat.SetTextureScale("_BaseMap", scale);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", r.name == "Worn concrete floor" ? .85f : 1.2f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", r.name == "Worn concrete floor" ? .29f : .18f);
            SurfaceCount++;

            // Only dress the two corridor-facing long wall surfaces. The meshes remain flush with their faces.
            if (r.name != "Stained plaster" || b.size.z < 2 || b.size.x > 1) continue;
            float x = b.center.x < centerX ? b.max.x + .002f : b.min.x - .002f;
            Vector3 normal = b.center.x < centerX ? Vector3.right : Vector3.left;
            Vector3 across = Vector3.forward;
            float z0 = Mathf.Max(b.min.z + .2f, planeZ - 2.8f), z1 = Mathf.Min(b.max.z - .2f, planeZ + 4.5f);
            if (z1 <= z0) continue;
            for (int i = 0; i < 7; i++)
            {
                float z = Mathf.Lerp(z0, z1, (i + .35f) / 7f);
                float y = floorY + Range(1.15f, 2.25f);
                Patch("Spalled corridor paint", new Vector3(x, y, z), across, Vector3.up, normal, Range(.08f, .25f), Range(.1f, .32f), exposed);
                if (i % 2 == 0) Strip("Water track below plaster seam", new Vector3(x + normal.x * .001f, y - .16f, z), across, Vector3.up, normal, Range(.007f, .014f), Range(.15f, .38f), damp);
            }
        }
    }

    void DressDoorwayCheeks()
    {
        // Sparse chips follow construction seams and corners, rather than an evenly spaced decorative pattern.
        float edge = frame ? frame.bounds.extents.x : .86f;
        float top = frame ? frame.bounds.max.y : floorY + 2.4f;
        Vector3[] anchors = {
            new Vector3(centerX - edge - .014f, top - .05f, planeZ + .127f),
            new Vector3(centerX - edge - .019f, floorY + .26f, planeZ + .127f),
            new Vector3(centerX + edge + .012f, top - .19f, planeZ + .127f),
            new Vector3(centerX + edge + .022f, floorY + .83f, planeZ + .127f)
        };
        for (int i = 0; i < anchors.Length; i++)
        {
            var p = anchors[i];
            float side = p.x < centerX ? -1 : 1;
            Patch("Small plaster chip at frame seam", p, Vector3.right, Vector3.up, Vector3.forward, Range(.016f, .030f), Range(.045f, .08f), exposed);
            if (i == 0 || i == 2)
            {
                Vector3 axis = new Vector3(side * .31f, 1, 0).normalized;
                Strip("Fine corner stress fracture", p + Vector3.forward * .001f + Vector3.up * .04f, Vector3.right, axis, Vector3.forward, .0018f, Range(.10f, .16f), damp);
                // One little broken flake beside the corner is deliberately asymmetric.
                Patch("Detached edge of old paint", p + new Vector3(side * .027f, -.047f, .001f), Vector3.right, Vector3.up, Vector3.forward, .012f, .022f, exposed);
            }
        }
    }

    public void ClawImpact(Vector3 worldPoint)
    {
        if (!detailRoot || ImpactCount >= MaximumImpacts) return;
        // Repeat calls at the same planted hand must not create damage every frame.
        foreach (var previous in impacts) if (Vector3.Distance(previous, worldPoint) < .07f) return;
        impacts.Add(worldPoint);
        ImpactCount++;
        float side = worldPoint.x < centerX ? -1 : 1;
        float jambX = worldPoint.x;
        if (frame) jambX = Mathf.Clamp(jambX, frame.bounds.min.x + .025f, frame.bounds.max.x - .025f);
        // Original rig contact proxy is 18cm above the visible claw band. Keep deduplication at the real proxy.
        Vector3 contact = new Vector3(jambX, Mathf.Clamp(worldPoint.y - .18f, floorY + .15f, floorY + 2.22f), frontZ + .005f);
        // Four uneven claw cuts: dark recessed core, fresh timber torn edges and several projecting fibres.
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = contact + Vector3.up * ((i - 1.5f) * .082f) + Vector3.right * Range(-.013f, .013f);
            Vector3 axis = new Vector3(side * Range(.25f, .55f), -.94f, 0).normalized;
            float length = Range(.12f, .23f);
            Strip("Claw gouge exposed timber " + ImpactCount + "/" + i, p, Vector3.right, axis, Vector3.forward, .026f, length, timber);
            Strip("Claw gouge dark core", p + Vector3.forward * .0025f, Vector3.right, axis, Vector3.forward, .009f, length * .91f, cavity);
            for (int j = 0; j < 2; j++)
            {
                var fibre = MeshObject("Raised splinter at claw cut", shavingMesh, timber);
                fibre.transform.position = p + axis * Range(-.04f, .04f) + Vector3.forward * .007f;
                fibre.transform.rotation = Quaternion.Euler(12 * side, 0, side * Range(12, 38));
                fibre.transform.localScale = new Vector3(Range(.004f, .007f), Range(.024f, .050f), .003f);
            }
            GougeCount++;
        }
        for (int i = 0; i < ShavingsPerImpact; i++)
        {
            var go = MeshObject("Falling wood shaving " + shavings.Count, shavingMesh, i % 4 == 0 ? cavity : timber);
            go.transform.position = contact + new Vector3(Range(-.025f, .025f), Range(-.12f, .12f), .012f);
            go.transform.rotation = Quaternion.Euler(Range(0, 360), Range(0, 360), Range(0, 360));
            go.transform.localScale = new Vector3(Range(.003f, .009f), Range(.014f, .052f), Range(.0015f, .004f));
            shavings.Add(new Shaving { transform = go.transform, velocity = new Vector3(side * Range(.02f, .15f), Range(.03f, .30f), Range(.07f, .30f)), spin = new Vector3(Range(-190, 190), Range(-160, 160), Range(-180, 180)), yaw = Range(0, 360) });
        }
    }

    void Update()
    {
        if (!detailRoot) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        foreach (var s in shavings)
        {
            if (s.settled || !s.transform) continue;
            s.velocity += Vector3.down * 6.8f * dt;
            s.velocity *= Mathf.Max(0, 1 - dt * .55f);
            s.transform.position += s.velocity * dt;
            s.transform.Rotate(s.spin * dt, Space.World);
            if (s.transform.position.y < floorY + .004f)
            {
                var p = s.transform.position; p.y = floorY + .004f; s.transform.position = p;
                s.transform.rotation = Quaternion.Euler(90, s.yaw, 0);
                s.settled = true;
            }
        }
    }

    GameObject Box(string label, Vector3 p, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = label; go.transform.SetParent(detailRoot, true); go.transform.position = p; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    GameObject MeshObject(string label, Mesh mesh, Material material)
    {
        var go = new GameObject(label);
        go.transform.SetParent(detailRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    void Patch(string label, Vector3 p, Vector3 across, Vector3 up, Vector3 normal, float width, float height, Material material)
    {
        // Broken paint has angular, undercut edges rather than circular blotches.
        var outline = new[] { new Vector2(-1,-.4f), new Vector2(-.55f,-1), new Vector2(.15f,-.65f), new Vector2(.85f,-.83f), new Vector2(.63f,.12f), new Vector2(.96f,.72f), new Vector2(-.35f,1), new Vector2(-.83f,.36f) };
        int count = outline.Length;
        var vertices = new Vector3[count + 1]; var uv = new Vector2[count + 1]; var triangles = new int[count * 3];
        vertices[0] = p; uv[0] = Vector2.one * .5f;
        for (int i = 0; i < count; i++)
        {
            Vector2 q = outline[i] * Range(.8f, 1);
            vertices[i + 1] = p + across * (q.x * width) + up * (q.y * height);
            uv[i + 1] = q * .5f + Vector2.one * .5f;
            triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % count + 1;
        }
        SurfaceMesh(label, vertices, uv, triangles, normal, material);
    }

    void Strip(string label, Vector3 p, Vector3 across, Vector3 axis, Vector3 normal, float width, float length, Material material)
    {
        const int segments = 6;
        var vertices = new Vector3[(segments + 1) * 2]; var uv = new Vector2[vertices.Length]; var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments, taper = .14f + Mathf.Sin(t * Mathf.PI) * .86f;
            Vector3 center = p + axis * ((t - .5f) * length) + across * Range(-width * .1f, width * .1f);
            vertices[i * 2] = center - across * width * taper * .5f; vertices[i * 2 + 1] = center + across * width * taper * .5f;
            uv[i * 2] = new Vector2(0, t); uv[i * 2 + 1] = new Vector2(1, t);
            if (i == segments) continue;
            int a = i * 2, k = i * 6; triangles[k] = a; triangles[k + 1] = a + 1; triangles[k + 2] = a + 2; triangles[k + 3] = a + 1; triangles[k + 4] = a + 3; triangles[k + 5] = a + 2;
        }
        SurfaceMesh(label, vertices, uv, triangles, normal, material);
    }

    void SurfaceMesh(string label, Vector3[] worldVertices, Vector2[] uv, int[] triangles, Vector3 normal, Material material)
    {
        var cross = Vector3.Cross(worldVertices[triangles[1]] - worldVertices[triangles[0]], worldVertices[triangles[2]] - worldVertices[triangles[0]]);
        if (Vector3.Dot(cross, normal) < 0) for (int i = 0; i < triangles.Length; i += 3) { int t = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = t; }
        var origin = worldVertices[0];
        for (int i = 0; i < worldVertices.Length; i++) worldVertices[i] -= origin;
        var mesh = new Mesh { name = label, vertices = worldVertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
        var go = MeshObject(label, mesh, material); go.transform.position = origin;
    }

    Mesh SplinterMesh()
    {
        var mesh = new Mesh { name = "Tapered timber shaving" };
        mesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.35f,-.4f,0), new Vector3(.5f,.24f,0), new Vector3(-.16f,.5f,0), new Vector3(0,0,.5f), new Vector3(0,0,-.5f) };
        mesh.uv = new[] { new Vector2(0,0),new Vector2(1,0),new Vector2(1,.8f),new Vector2(.2f,1),new Vector2(.5f,.5f),new Vector2(.5f,.5f) };
        mesh.triangles = new[] {0,1,4,1,2,4,2,3,4,3,0,4,1,0,5,2,1,5,3,2,5,0,3,5};
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh); return mesh;
    }
    float Range(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
    void OnDestroy()
    {
        foreach (var s in originalSurfaces) if (s.renderer) s.renderer.sharedMaterial = s.material;
        if (detailRoot) Destroy(detailRoot.gameObject);
        foreach (var m in ownedMeshes) if (m) Destroy(m);
        foreach (var m in ownedMaterials) if (m) Destroy(m);
    }
}
