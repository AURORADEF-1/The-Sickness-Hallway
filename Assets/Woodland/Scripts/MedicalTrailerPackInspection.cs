using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// A reversible, presentation-only check of the existing field kit. The normal
// inventory, medical state and save file are never consumed by this trailer beat.
[DefaultExecutionOrder(1250)]
public class MedicalTrailerPackInspection : MonoBehaviour {
    public const float CloseAt = 4.5f;
    public const float LifeTime = 5.15f;
    public bool Opened { get; private set; }
    public bool EmptyBatterySlotsVisible { get; private set; }
    public bool NoSpareCells => inventory && inventory.Count("torch_cell") == 0;
    public bool Finished { get; private set; }
    // Inventory selection stays unobstructed; retained for recorder telemetry.
    public bool HandVisible => false;
    public int EmptySlots => 3;
    public float Age { get; private set; }

    WoodlandPlayer player;
    WoodlandInventory inventory;
    WoodlandFieldKitUI kit;
    WoodlandPhysicalPack physical;
    FieldSave originalData;
    bool originalPersist, physicalEnabled, closing, disposed;
    string originalStatus;
    Transform slots, closingContents;
    readonly List<Material> ownedMaterials = new List<Material>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    readonly List<Canvas> hiddenCanvases = new List<Canvas>();

    public bool Begin(WoodlandPlayer p) {
        if (Opened || originalData != null) return Opened;
        player = p;
        if (!player || !player.inventory || !player.fieldKit || !player.pack) return false;
        inventory = player.inventory;
        kit = player.fieldKit;
        if (kit.Busy) return false;
        originalData = inventory.data;
        originalPersist = inventory.persist;
        originalStatus = inventory.status;
        inventory.persist = false;
        inventory.data = JsonConvert.DeserializeObject<FieldSave>(JsonConvert.SerializeObject(originalData));
        inventory.data.items.RemoveAll(i => i.kind == "torch_cell" || i.kind == "556");
        if (inventory.anatomy) inventory.anatomy.condition = inventory.data.health;
        bool locked = player.cinematicLocked;
        player.cinematicLocked = false;
        Opened = kit.Toggle();
        player.cinematicLocked = locked;
        if (!Opened) { Dispose(); return false; }
        physical = kit.GetComponent<WoodlandPhysicalPack>();
        if (physical) { physicalEnabled = physical.enabled; physical.enabled = false; }
        // The contents are still the normal physical inventory, just arranged
        // away from the empty battery organizer so the action reads without text.
        ArrangeContents();
        BuildEmptyOrganizer();
        HideCanvases();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        return true;
    }

    public void Tick(float age) {
        if (!Opened || disposed || Finished) return;
        Age = Mathf.Max(0, age);
        HideCanvases();
        if (Age >= CloseAt) Close();
        if (closing && !kit.Busy) {
            Finished = true;
            EmptyBatterySlotsVisible = false;
            if (slots) slots.gameObject.SetActive(false);
            if (closingContents) closingContents.gameObject.SetActive(false);
            if (physical) physical.enabled = physicalEnabled;
        }
    }

    void ArrangeContents() {
        var tray = player.pack.Find("Physical backpack contents");
        if (!tray) return;
        foreach (var item in tray.GetComponentsInChildren<WoodlandPackItem>(true)) {
            var data = inventory.data.items.Find(i => i.id == item.id);
            if (data == null) continue;
            if (data.kind == "rifle" || data.kind == "notebook" || data.kind == "map") {
                item.gameObject.SetActive(false);
                continue;
            }
            // Keep medical supplies and working gear in their actual pockets.
            Vector3 pos = item.transform.localPosition;
            if (data.kind == "medical") pos = new Vector3(-.12f, .285f, -.10f);
            else if (data.kind == "filter") pos = new Vector3(.12f, .285f, -.10f);
            else if (data.kind == "suit_patch") pos = new Vector3(0, .285f, -.115f);
            else if (data.kind == "radio") pos = new Vector3(-.15f, .285f, .045f);
            else if (data.kind == "torch") pos = new Vector3(.15f, .285f, .045f);
            item.transform.localPosition = pos;
        }
    }

    Material Material(Color color, float metallic = 0) {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", metallic > 0 ? .38f : .12f);
        ownedMaterials.Add(mat);
        return mat;
    }

    Transform Part(string name, Vector3 position, Vector3 scale, Material mat) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(slots, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var collider = go.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        return go.transform;
    }

    void BuildEmptyOrganizer() {
        slots = new GameObject("Trailer empty spare battery organizer").transform;
        slots.SetParent(player.pack, false);
        slots.localRotation = Quaternion.Euler(0, 180, 0);
        slots.localPosition = new Vector3(0, .29f, -.065f);
        var canvas = Material(new Color(.12f, .135f, .075f));
        var rubber = Material(new Color(.022f, .026f, .019f));
        var stitching = Material(new Color(.27f, .28f, .17f));
        var metal = Material(new Color(.30f, .32f, .27f), .75f);
        Part("Empty fabric battery pouch", Vector3.zero, new Vector3(.145f, .009f, .135f), canvas);
        for (int i = 0; i < 3; i++) {
            float x = (i - 1) * .043f;
            Part("Empty recessed cell channel " + i, new Vector3(x, .006f, 0), new Vector3(.034f, .004f, .114f), rubber);
            Part("Battery channel seam " + i, new Vector3(x - .019f, .010f, 0), new Vector3(.003f, .009f, .124f), stitching);
            Part("Exposed lower contact " + i, new Vector3(x, .009f, -.052f), new Vector3(.018f, .004f, .009f), metal);
            Part("Exposed upper contact " + i, new Vector3(x, .009f, .052f), new Vector3(.018f, .004f, .009f), metal);
            ElasticLoop(x, -.025f, canvas);
            ElasticLoop(x, .025f, canvas);
        }
        EmptyBatterySlotsVisible = true;
    }

    void ElasticLoop(float x, float z, Material mat) {
        const int segments = 12;
        var vertices = new Vector3[(segments + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 12];
        for (int i = 0; i <= segments; i++) {
            float u = i / (float)segments, angle = u * Mathf.PI;
            float px = x - Mathf.Cos(angle) * .019f;
            float py = .010f + Mathf.Sin(angle) * .016f;
            vertices[i * 2] = new Vector3(px, py, z - .006f);
            vertices[i * 2 + 1] = new Vector3(px, py, z + .006f);
            uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
            if (i == segments) continue;
            int a = i * 2, t = i * 12;
            triangles[t] = a; triangles[t + 1] = a + 1; triangles[t + 2] = a + 2;
            triangles[t + 3] = a + 1; triangles[t + 4] = a + 3; triangles[t + 5] = a + 2;
            triangles[t + 6] = a + 2; triangles[t + 7] = a + 1; triangles[t + 8] = a;
            triangles[t + 9] = a + 2; triangles[t + 10] = a + 3; triangles[t + 11] = a + 1;
        }
        var mesh = new Mesh { name = "Hollow elastic battery loop", vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
        var go = new GameObject("Unfilled elastic cell loop", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(slots, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    void HideCanvases() {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) {
            if (!canvas.enabled) continue;
            if (!hiddenCanvases.Contains(canvas)) hiddenCanvases.Add(canvas);
            canvas.enabled = false;
        }
    }

    public void Close() {
        if (!Opened || closing) return;
        closing = true;
        // FieldKit.Close destroys its temporary tray. Preserve a static visual
        // through the standing-up blend rather than popping all supplies away.
        var tray = player.pack.Find("Physical backpack contents");
        if (tray) {
            closingContents = Instantiate(tray, player.pack, false);
            closingContents.name = "Trailer contents during pack close";
            foreach (var collider in closingContents.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; Destroy(collider); }
            foreach (var script in closingContents.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (var renderer in closingContents.GetComponentsInChildren<Renderer>(true)) {
                var source = renderer.sharedMaterials;
                var copies = new Material[source.Length];
                for (int i = 0; i < source.Length; i++) {
                    if (!source[i]) continue;
                    copies[i] = new Material(source[i]); ownedMaterials.Add(copies[i]);
                }
                renderer.sharedMaterials = copies;
            }
        }
        kit.Close();
    }

    public void Dispose() {
        if (disposed) return;
        disposed = true;
        if (kit && kit.Open) kit.Close();
        if (physical) physical.enabled = physicalEnabled;
        if (inventory && originalData != null) {
            inventory.data = originalData;
            inventory.persist = originalPersist;
            inventory.status = originalStatus;
            if (inventory.anatomy) inventory.anatomy.condition = originalData.health;
            inventory.Notify("");
        }
        foreach (var canvas in hiddenCanvases) if (canvas) canvas.enabled = true;
        hiddenCanvases.Clear();
        if (slots) Destroy(slots.gameObject);
        if (closingContents) Destroy(closingContents.gameObject);
        foreach (var mat in ownedMaterials) if (mat) Destroy(mat);
        foreach (var mesh in ownedMeshes) if (mesh) Destroy(mesh);
        ownedMaterials.Clear(); ownedMeshes.Clear();
    }

    void OnDestroy() { Dispose(); }
}
