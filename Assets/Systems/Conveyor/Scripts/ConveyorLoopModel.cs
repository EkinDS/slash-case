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
        var topLeft = new Vector2(boardRect.xMin - padding, boardRect.yMax + padding);
        var topRight = new Vector2(boardRect.xMax + padding, boardRect.yMax + padding);
        var bottomRight = new Vector2(boardRect.xMax + padding, boardRect.yMin - padding);
        var bottomLeft = new Vector2(boardRect.xMin - padding, boardRect.yMin - padding);

        if (wrappedDistance < width)
        {
            return Vector2.Lerp(topLeft, topRight, width <= 0F ? 0F : wrappedDistance / width);
        }

        wrappedDistance -= width;

        if (wrappedDistance < height)
        {
            return Vector2.Lerp(topRight, bottomRight, height <= 0F ? 0F : wrappedDistance / height);
        }

        wrappedDistance -= height;

        if (wrappedDistance < width)
        {
            return Vector2.Lerp(bottomRight, bottomLeft, width <= 0F ? 0F : wrappedDistance / width);
        }

        wrappedDistance -= width;
        return Vector2.Lerp(bottomLeft, topLeft, height <= 0F ? 0F : wrappedDistance / height);
    }

    public ConveyorSide EvaluateSide(float distance)
    {
        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < width)
        {
            return ConveyorSide.Top;
        }

        wrappedDistance -= width;

        if (wrappedDistance < height)
        {
            return ConveyorSide.Right;
        }

        wrappedDistance -= height;

        if (wrappedDistance < width)
        {
            return ConveyorSide.Bottom;
        }

        return ConveyorSide.Left;
    }

    public int EvaluateLineIndex(float distance, int gridWidth, int gridHeight)
    {
        var wrappedDistance = WrapDistance(distance);

        if (wrappedDistance < width)
        {
            return NormalizeLineIndex(boardRect.xMin, boardRect.width, EvaluatePosition(distance).x, gridWidth);
        }

        wrappedDistance -= width;

        if (wrappedDistance < height)
        {
            return NormalizeLineIndex(boardRect.yMax, -boardRect.height, EvaluatePosition(distance).y, gridHeight);
        }

        wrappedDistance -= height;

        if (wrappedDistance < width)
        {
            return NormalizeLineIndex(boardRect.xMax, -boardRect.width, EvaluatePosition(distance).x, gridWidth);
        }

        return NormalizeLineIndex(boardRect.yMin, boardRect.height, EvaluatePosition(distance).y, gridHeight);
    }

    private static int NormalizeLineIndex(float axisStart, float axisLength, float axisValue, int lineCount)
    {
        if (lineCount <= 1 || Mathf.Abs(axisLength) < 0.001F)
        {
            return 0;
        }

        var normalized = Mathf.Clamp01((axisValue - axisStart) / axisLength);
        return Mathf.Clamp(Mathf.RoundToInt(normalized * (lineCount - 1)), 0, lineCount - 1);
    }
}
