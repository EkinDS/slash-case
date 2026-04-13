using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PixelGridView : MonoBehaviour, IPixelGridView
{
    private const float WorldCellSize = 1F;
    private const float CellVisualScale = 0.85F;
    private readonly Dictionary<int, GameObject> cellObjects = new Dictionary<int, GameObject>();
    private readonly List<GameObject> conveyorObjects = new List<GameObject>();

    private Transform boardRoot;
    private Transform effectsRoot;
    private TextMeshPro conveyorCapacityText;
    private int width;
    private int height;
    private float cellSize;
    private float cellGap;
    private float renderedCellSize;
    private Vector3 boardCenter;
    private Vector2 boardSize;

    public event Action<int, int> CellClicked;

    public Rect BoardRect => new Rect(boardCenter.x - boardSize.x * 0.5F, boardCenter.z - boardSize.y * 0.5F, boardSize.x, boardSize.y);

    public Transform PigRoot { get; private set; }

    public void Initialize(Transform parent)
    {
        transform.SetParent(parent, false);
        transform.position = Vector3.zero;

        boardRoot = new GameObject("BoardRoot").transform;
        boardRoot.SetParent(transform, false);

        effectsRoot = new GameObject("EffectsRoot").transform;
        effectsRoot.SetParent(transform, false);

        PigRoot = new GameObject("PigRoot").transform;
        PigRoot.SetParent(transform, false);
    }

    public void BuildGrid(int width, int height)
    {
        StopAllCoroutines();

        this.width = Mathf.Max(1, width);
        this.height = Mathf.Max(1, height);

        foreach (var pair in cellObjects)
        {
            Destroy(pair.Value);
        }

        foreach (var conveyorObject in conveyorObjects)
        {
            Destroy(conveyorObject);
        }

        if (conveyorCapacityText != null)
        {
            Destroy(conveyorCapacityText.gameObject);
            conveyorCapacityText = null;
        }

        cellObjects.Clear();
        conveyorObjects.Clear();

        cellGap = 0F;
        cellSize = WorldCellSize;
        renderedCellSize = cellSize * CellVisualScale;
        boardSize = new Vector2(
            this.width * cellSize + (this.width - 1) * cellGap,
            this.height * cellSize + (this.height - 1) * cellGap);
        boardCenter = new Vector3(0F, 0F, 0F);

        BuildConveyorVisuals();

        for (var y = 0; y < this.height; y++)
        {
            for (var x = 0; x < this.width; x++)
            {
                var cellObject = WorldObjectUtility.CreatePrimitive(
                    $"Cell_{x}_{y}",
                    PrimitiveType.Cube,
                    boardRoot,
                    GetCellLocalPosition(x, y),
                    new Vector3(renderedCellSize, renderedCellSize, renderedCellSize),
                    new Color32(44, 52, 74, 255));

                var relay = cellObject.AddComponent<ClickRelay>();
                var localX = x;
                var localY = y;
                relay.Clicked += () => CellClicked?.Invoke(localX, localY);
                cellObjects.Add(GetCellKey(x, y), cellObject);
            }
        }
    }

    public void RenderCell(int x, int y, PixelPigColor color, bool alive)
    {
        if (!cellObjects.TryGetValue(GetCellKey(x, y), out var cellObject))
        {
            return;
        }

        var renderer = cellObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.enabled = alive;
        }

        WorldObjectUtility.SetColor(cellObject, color.ToUnityColor());
        cellObject.transform.localScale = new Vector3(renderedCellSize, renderedCellSize, renderedCellSize);
        cellObject.transform.localPosition = GetCellLocalPosition(x, y);
    }

    public Vector2 GetCellCenter(int x, int y)
    {
        var worldPosition = boardRoot.TransformPoint(GetCellLocalPosition(x, y) + new Vector3(0F, 0.22F, 0F));
        return new Vector2(worldPosition.x, worldPosition.z);
    }

    public void SetEditorSelection(PixelPigColor color)
    {
    }

    public void PlayShot(Vector2 from, Vector2 to, PixelPigColor color, Action onImpact)
    {
        var shotObject = WorldObjectUtility.CreatePrimitive(
            "Shot",
            PrimitiveType.Sphere,
            effectsRoot,
            new Vector3(from.x, 0.72F, from.y),
            Vector3.one * 0.22F);
        WorldObjectUtility.SetColor(shotObject, color.ToUnityColor());
        Destroy(shotObject.GetComponent<Collider>());
        StartCoroutine(ShotRoutine(shotObject, from, to, onImpact));
    }

    public void PlayCellHit(int x, int y, Action onComplete)
    {
        if (cellObjects.TryGetValue(GetCellKey(x, y), out var cellObject))
        {
            StartCoroutine(CellHitRoutine(cellObject, onComplete));
            return;
        }

        onComplete?.Invoke();
    }

    public void SetConveyorCapacity(int remainingCapacity, int totalCapacity)
    {
        if (conveyorCapacityText == null)
        {
            return;
        }

        conveyorCapacityText.text = $"{Mathf.Max(0, remainingCapacity)}/{Mathf.Max(0, totalCapacity)}";
    }

    private void BuildConveyorVisuals()
    {
        var cornerGap = 1F;
        CreateConveyorStrip("TopConveyor", new Vector3(boardSize.x + 2.4F, 0.16F, 0.45F), new Vector3(0F, 0F, boardSize.y * 0.5F + 1F));
        CreateConveyorStrip("BottomConveyor", new Vector3(boardSize.x + 2.4F - cornerGap, 0.16F, 0.45F),
            new Vector3(cornerGap * 0.5F, 0F, -boardSize.y * 0.5F - 1F));
        CreateConveyorStrip("LeftConveyor", new Vector3(0.45F, 0.16F, boardSize.y + 2.4F - cornerGap),
            new Vector3(-boardSize.x * 0.5F - 1F, 0F, cornerGap * 0.5F));
        CreateConveyorStrip("RightConveyor", new Vector3(0.45F, 0.16F, boardSize.y + 2.4F), new Vector3(boardSize.x * 0.5F + 1F, 0F, 0F));

        var entryPad = WorldObjectUtility.CreatePrimitive(
            "ConveyorEntryPad",
            PrimitiveType.Cube,
            transform,
            new Vector3(-boardSize.x * 0.5F - 1F, -0.24F, -boardSize.y * 0.5F - 1F),
            new Vector3(0.95F, 0.18F, 0.95F),
            new Color32(210, 220, 232, 255));
        conveyorObjects.Add(entryPad);

        conveyorCapacityText = WorldObjectUtility.CreateWorldText(
            "ConveyorCapacity",
            transform,
            new Vector3(-boardSize.x * 0.5F - 1F, 0.45F, -boardSize.y * 0.5F - 1F),
            "5/5",
            72,
            Color.black,
            TextAnchor.MiddleCenter,
            0.11F);
        conveyorCapacityText.transform.localRotation = Quaternion.Euler(90F, 0F, 0F);
    }

    private void CreateConveyorStrip(string name, Vector3 scale, Vector3 localPosition)
    {
        var conveyorObject = WorldObjectUtility.CreatePrimitive(
            name,
            PrimitiveType.Cube,
            transform,
            localPosition + new Vector3(0F, -0.25F, 0F),
            scale,
            WorldMaterialRole.Conveyor);
        conveyorObjects.Add(conveyorObject);
    }

    private IEnumerator ShotRoutine(GameObject shotObject, Vector2 from, Vector2 to, Action onImpact)
    {
        var distance = Vector2.Distance(from, to);
        var duration = Mathf.Max(0.01F, distance / 30F);
        var elapsed = 0F;
        var start = new Vector3(from.x, 0.72F, from.y);
        var end = new Vector3(to.x, 0.35F, to.y);

        while (elapsed < duration)
        {
            if (shotObject == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.35F;
            shotObject.transform.position = position;
            yield return null;
        }

        if (shotObject == null)
        {
            yield break;
        }

        shotObject.transform.position = end;
        onImpact?.Invoke();
        Destroy(shotObject);
    }

    private IEnumerator CellHitRoutine(GameObject cellObject, Action onComplete)
    {
        if (cellObject == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        var originalScale = cellObject.transform.localScale;
        var originalPosition = cellObject.transform.localPosition;
        var originalRotation = cellObject.transform.localRotation;
        const float duration = 0.15F;
        const float targetScaleMultiplier = 1.5F;
        const float shakeAmplitude = 0.08F;
        const float shakeFrequency = 26F;
        const float leftTiltDegrees = 5F;
        const float rightTiltDegrees = 10F;
        var elapsed = 0F;

        while (elapsed < duration)
        {
            if (cellObject == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var scaleMultiplier = Mathf.Lerp(1F, targetScaleMultiplier, t);
            var shakeOffset = Mathf.Sin(t * Mathf.PI * shakeFrequency) * shakeAmplitude * (1F - t * 0.15F);

            cellObject.transform.localScale = new Vector3(
                originalScale.x * scaleMultiplier,
                originalScale.y * scaleMultiplier,
                originalScale.z * scaleMultiplier);
            cellObject.transform.localPosition = originalPosition + new Vector3(0F, 0F, shakeOffset);
            var tiltDegrees = t < 0.5F
                ? Mathf.Lerp(0F, leftTiltDegrees, t / 0.5F)
                : Mathf.Lerp(leftTiltDegrees, -rightTiltDegrees, (t - 0.5F) / 0.5F);
            cellObject.transform.localRotation = originalRotation * Quaternion.Euler(0F, 0F, tiltDegrees);
            yield return null;
        }

        if (cellObject != null)
        {
            cellObject.transform.localRotation = originalRotation;
        }

        onComplete?.Invoke();
    }

    private Vector3 GetCellLocalPosition(int x, int y)
    {
        var startX = -boardSize.x * 0.5F + cellSize * 0.5F;
        var startZ = boardSize.y * 0.5F - cellSize * 0.5F;
        return new Vector3(startX + x * (cellSize + cellGap), 0F, startZ - y * (cellSize + cellGap));
    }

    private static int GetCellKey(int x, int y)
    {
        return x * 1000 + y;
    }
}
