using UnityEngine;

public sealed class ConveyorLoopModel
{
    private const float CornerGap = 1F;

    private readonly Rect boardRect;
    private readonly float padding;
    private readonly float width;
    private readonly float height;
    private readonly float bottomSegmentLength;
    private readonly float leftSegmentLength;

    public ConveyorLoopModel(Rect boardRect, float padding)
    {
        this.boardRect = boardRect;
        this.padding = padding;
        width = boardRect.width + padding * 2F;
        height = boardRect.height + padding * 2F;
        bottomSegmentLength = Mathf.Max(0.001F, width - CornerGap);
        leftSegmentLength = Mathf.Max(0.001F, height - CornerGap);
        LoopLength = bottomSegmentLength + height + width + leftSegmentLength;
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
        var bottomLeft = new Vector2(boardRect.xMin - padding, boardRect.yMin - padding);
        var startPoint = new Vector2(bottomLeft.x + CornerGap, bottomLeft.y);
        var finishPoint = new Vector2(bottomLeft.x, bottomLeft.y + CornerGap);
        var bottomRight = new Vector2(boardRect.xMax + padding, boardRect.yMin - padding);
        var topRight = new Vector2(boardRect.xMax + padding, boardRect.yMax + padding);
        var topLeft = new Vector2(boardRect.xMin - padding, boardRect.yMax + padding);

        if (distance < 0F)
        {
            return bottomLeft + new Vector2(0F, distance);
        }

        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < bottomSegmentLength)
        {
            return Vector2.Lerp(startPoint, bottomRight, bottomSegmentLength <= 0F ? 0F : wrappedDistance / bottomSegmentLength);
        }

        wrappedDistance -= bottomSegmentLength;

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
        return Vector2.Lerp(topLeft, finishPoint, leftSegmentLength <= 0F ? 0F : wrappedDistance / leftSegmentLength);
    }

    public ConveyorSide EvaluateSide(float distance)
    {
        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < bottomSegmentLength)
        {
            return ConveyorSide.Bottom;
        }

        wrappedDistance -= bottomSegmentLength;

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
        if (distance < 0F)
        {
            return 0;
        }

        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < bottomSegmentLength)
        {
            return NormalizeNormalizedDistance(bottomSegmentLength <= 0F ? 0F : wrappedDistance / bottomSegmentLength, gridWidth);
        }

        wrappedDistance -= bottomSegmentLength;

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
        return NormalizeNormalizedDistance(leftSegmentLength <= 0F ? 0F : wrappedDistance / leftSegmentLength, gridHeight);
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
