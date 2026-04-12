using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PixelGridView : MonoBehaviour, IPixelGridView
{
    private const float WorldCellSize = 1F;
    private const float AliveCellHeight = 0.16F;
    private const float DeadCellHeight = 0.05F;

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
        renderedCellSize = cellSize;
        boardSize = new Vector2(
            this.width * cellSize + (this.width - 1) * cellGap,
            this.height * cellSize + (this.height - 1) * cellGap);
        boardCenter = new Vector3(0F, 0F, 0F);

        var boardBase = WorldObjectUtility.CreatePrimitive(
            "BoardBase",
            PrimitiveType.Cube,
            boardRoot,
            new Vector3(0F, -0.18F, 0F),
            new Vector3(boardSize.x + 0.6F, 0.25F, boardSize.y + 0.6F),
            WorldMaterialRole.BoardBase);
        conveyorObjects.Add(boardBase);

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
                    new Vector3(renderedCellSize, AliveCellHeight, renderedCellSize),
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

        WorldObjectUtility.SetColor(cellObject, alive ? color.ToUnityColor() : new Color32(44, 52, 74, 180));
        cellObject.transform.localScale = new Vector3(renderedCellSize, alive ? AliveCellHeight : DeadCellHeight, renderedCellSize);
        cellObject.transform.localPosition = GetCellLocalPosition(x, y) + new Vector3(0F, alive ? 0F : -0.055F, 0F);
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

    public void PlayCellHit(int x, int y)
    {
        if (cellObjects.TryGetValue(GetCellKey(x, y), out var cellObject))
        {
            StartCoroutine(CellHitRoutine(cellObject));
        }
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
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.35F;
            shotObject.transform.position = position;
            yield return null;
        }

        shotObject.transform.position = end;
        onImpact?.Invoke();
        Destroy(shotObject);
    }

    private IEnumerator CellHitRoutine(GameObject cellObject)
    {
        var originalScale = cellObject.transform.localScale;
        var originalColor = cellObject.GetComponent<Renderer>().material.color;
        WorldObjectUtility.SetColor(cellObject, Color.white);
        cellObject.transform.localScale = originalScale + new Vector3(0.08F, 0.04F, 0.08F);
        yield return new WaitForSeconds(0.07F);
        WorldObjectUtility.SetColor(cellObject, originalColor);
        cellObject.transform.localScale = originalScale;
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
