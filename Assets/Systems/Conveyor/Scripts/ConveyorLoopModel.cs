using UnityEngine;

public sealed class ConveyorLoopModel
{
    private readonly Rect boardRect;
    private readonly float padding;
    private readonly float width;
    private readonly float height;

    public ConveyorLoopModel(Rect boardRect, float padding)
    {
        this.boardRect = boardRect;
        this.padding = padding;
        width = boardRect.width + padding * 2F;
        height = boardRect.height + padding * 2F;
        LoopLength = width * 2F + height * 2F;
    }

    public float LoopLength { get; }

    public float WrapDistance(float distance)
    {
        if (LoopLength <= 0F)
        {
            return 0F;
        }

        while (distance < 0F)
        {
            distance += LoopLength;
        }

        while (distance >= LoopLength)
        {
            distance -= LoopLength;
        }

        return distance;
    }

    public Vector2 EvaluatePosition(float distance)
    {
        var wrappedDistance = WrapDistance(distance);
        var bottomLeft = new Vector2(boardRect.xMin - padding, boardRect.yMin - padding);
        var bottomRight = new Vector2(boardRect.xMax + padding, boardRect.yMin - padding);
        var topRight = new Vector2(boardRect.xMax + padding, boardRect.yMax + padding);
        var topLeft = new Vector2(boardRect.xMin - padding, boardRect.yMax + padding);

        if (wrappedDistance < width)
        {
            return Vector2.Lerp(bottomLeft, bottomRight, width <= 0F ? 0F : wrappedDistance / width);
        }

        wrappedDistance -= width;

        if (wrappedDistance < height)
        {
            return Vector2.Lerp(bottomRight, topRight, height <= 0F ? 0F : wrappedDistance / height);
        }

        wrappedDistance -= height;

        if (wrappedDistance < width)
        {
            return Vector2.Lerp(topRight, topLeft, width <= 0F ? 0F : wrappedDistance / width);
        }

        wrappedDistance -= width;
        return Vector2.Lerp(topLeft, bottomLeft, height <= 0F ? 0F : wrappedDistance / height);
    }

    public ConveyorSide EvaluateSide(float distance)
    {
        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < width)
        {
            return ConveyorSide.Bottom;
        }

        wrappedDistance -= width;

        if (wrappedDistance < height)
        {
            return ConveyorSide.Right;
        }

        wrappedDistance -= height;

        if (wrappedDistance < width)
        {
            return ConveyorSide.Top;
        }

        return ConveyorSide.Left;
    }

    public int EvaluateLineIndex(float distance, int gridWidth, int gridHeight)
    {
        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < width)
        {
            return NormalizeNormalizedDistance(width <= 0F ? 0F : wrappedDistance / width, gridWidth);
        }

        wrappedDistance -= width;

        if (wrappedDistance < height)
        {
            return NormalizeNormalizedDistance(height <= 0F ? 0F : 1F - (wrappedDistance / height), gridHeight);
        }

        wrappedDistance -= height;

        if (wrappedDistance < width)
        {
            return NormalizeNormalizedDistance(width <= 0F ? 0F : 1F - (wrappedDistance / width), gridWidth);
        }

        wrappedDistance -= width;
        return NormalizeNormalizedDistance(height <= 0F ? 0F : wrappedDistance / height, gridHeight);
    }

    private static int NormalizeNormalizedDistance(float normalized, int lineCount)
    {
        if (lineCount <= 1)
        {
            return 0;
        }

        return Mathf.Clamp(Mathf.RoundToInt(normalized * (lineCount - 1)), 0, lineCount - 1);
    }
}
