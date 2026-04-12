using System;
using System.Collections.Generic;
using System.Text;

public static class PigLineGenerator
{
    private const int FixedBoardSize = 20;

    public static List<PigSpawnData> GenerateSolvableSequence(PixelFlowLevelData levelData)
    {
        var grid = BuildGrid(levelData);
        var memo = new Dictionary<string, List<PigSpawnData>>();
        return TrySolve(grid, memo, out var sequence) ? sequence : BuildFallbackSequence(grid);
    }

    private static bool TrySolve(PixelPigColor[,] grid, Dictionary<string, List<PigSpawnData>> memo, out List<PigSpawnData> sequence)
    {
        if (!HasAnyCells(grid))
        {
            sequence = new List<PigSpawnData>();
            return true;
        }

        var key = BuildKey(grid);
        if (memo.TryGetValue(key, out var cached))
        {
            sequence = cached != null ? new List<PigSpawnData>(cached) : null;
            return cached != null;
        }

        var candidates = GetCandidateColors(grid);
        candidates.Sort((left, right) => right.removedCount.CompareTo(left.removedCount));

        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            var nextGrid = CloneGrid(grid);
            StripExposedColor(nextGrid, candidate.color);

            if (!TrySolve(nextGrid, memo, out var tail))
            {
                continue;
            }

            sequence = new List<PigSpawnData>(tail.Count + 1)
            {
                new PigSpawnData(candidate.color, candidate.removedCount)
            };
            sequence.AddRange(tail);
            memo[key] = new List<PigSpawnData>(sequence);
            return true;
        }

        memo[key] = null;
        sequence = null;
        return false;
    }

    private static List<(PixelPigColor color, int removedCount)> GetCandidateColors(PixelPigColor[,] grid)
    {
        var candidates = new List<(PixelPigColor color, int removedCount)>();

        foreach (PixelPigColor color in Enum.GetValues(typeof(PixelPigColor)))
        {
            if (color == PixelPigColor.None)
            {
                continue;
            }

            var previewGrid = CloneGrid(grid);
            var removedCount = StripExposedColor(previewGrid, color);

            if (removedCount > 0)
            {
                candidates.Add((color, removedCount));
            }
        }

        return candidates;
    }

    private static int StripExposedColor(PixelPigColor[,] grid, PixelPigColor color)
    {
        var totalRemoved = 0;

        while (true)
        {
            var exposed = GetExposedCells(grid, color);

            if (exposed.Count == 0)
            {
                return totalRemoved;
            }

            for (var i = 0; i < exposed.Count; i++)
            {
                var position = exposed[i];
                grid[position.x, position.y] = PixelPigColor.None;
            }

            totalRemoved += exposed.Count;
        }
    }

    private static List<(int x, int y)> GetExposedCells(PixelPigColor[,] grid, PixelPigColor targetColor)
    {
        var result = new List<(int x, int y)>();
        var seen = new HashSet<int>();

        for (var x = 0; x < FixedBoardSize; x++)
        {
            AddFirstVisibleInColumn(grid, x, 0, FixedBoardSize, 1, targetColor, seen, result);
            AddFirstVisibleInColumn(grid, x, FixedBoardSize - 1, -1, -1, targetColor, seen, result);
        }

        for (var y = 0; y < FixedBoardSize; y++)
        {
            AddFirstVisibleInRow(grid, y, 0, FixedBoardSize, 1, targetColor, seen, result);
            AddFirstVisibleInRow(grid, y, FixedBoardSize - 1, -1, -1, targetColor, seen, result);
        }

        return result;
    }

    private static void AddFirstVisibleInColumn(PixelPigColor[,] grid, int x, int startY, int endExclusive, int step,
        PixelPigColor targetColor, HashSet<int> seen, List<(int x, int y)> result)
    {
        for (var y = startY; y != endExclusive; y += step)
        {
            var color = grid[x, y];

            if (color == PixelPigColor.None)
            {
                continue;
            }

            if (color == targetColor)
            {
                AddCell(x, y, seen, result);
            }

            return;
        }
    }

    private static void AddFirstVisibleInRow(PixelPigColor[,] grid, int y, int startX, int endExclusive, int step,
        PixelPigColor targetColor, HashSet<int> seen, List<(int x, int y)> result)
    {
        for (var x = startX; x != endExclusive; x += step)
        {
            var color = grid[x, y];

            if (color == PixelPigColor.None)
            {
                continue;
            }

            if (color == targetColor)
            {
                AddCell(x, y, seen, result);
            }

            return;
        }
    }

    private static void AddCell(int x, int y, HashSet<int> seen, List<(int x, int y)> result)
    {
        var key = x * 1000 + y;

        if (!seen.Add(key))
        {
            return;
        }

        result.Add((x, y));
    }

    private static PixelPigColor[,] BuildGrid(PixelFlowLevelData levelData)
    {
        var grid = new PixelPigColor[FixedBoardSize, FixedBoardSize];

        if (levelData?.cells == null)
        {
            return grid;
        }

        for (var i = 0; i < levelData.cells.Length; i++)
        {
            var cell = levelData.cells[i];

            if (cell == null || cell.color == PixelPigColor.None)
            {
                continue;
            }

            if (cell.x < 0 || cell.x >= FixedBoardSize || cell.y < 0 || cell.y >= FixedBoardSize)
            {
                continue;
            }

            grid[cell.x, cell.y] = cell.color;
        }

        return grid;
    }

    private static PixelPigColor[,] CloneGrid(PixelPigColor[,] source)
    {
        var clone = new PixelPigColor[FixedBoardSize, FixedBoardSize];

        for (var x = 0; x < FixedBoardSize; x++)
        {
            for (var y = 0; y < FixedBoardSize; y++)
            {
                clone[x, y] = source[x, y];
            }
        }

        return clone;
    }

    private static bool HasAnyCells(PixelPigColor[,] grid)
    {
        for (var x = 0; x < FixedBoardSize; x++)
        {
            for (var y = 0; y < FixedBoardSize; y++)
            {
                if (grid[x, y] != PixelPigColor.None)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string BuildKey(PixelPigColor[,] grid)
    {
        var builder = new StringBuilder(FixedBoardSize * FixedBoardSize);

        for (var y = 0; y < FixedBoardSize; y++)
        {
            for (var x = 0; x < FixedBoardSize; x++)
            {
                builder.Append((char)('A' + (int)grid[x, y] + 1));
            }
        }

        return builder.ToString();
    }

    private static List<PigSpawnData> BuildFallbackSequence(PixelPigColor[,] grid)
    {
        var counts = new Dictionary<PixelPigColor, int>();

        foreach (PixelPigColor color in Enum.GetValues(typeof(PixelPigColor)))
        {
            if (color != PixelPigColor.None)
            {
                counts[color] = 0;
            }
        }

        for (var x = 0; x < FixedBoardSize; x++)
        {
            for (var y = 0; y < FixedBoardSize; y++)
            {
                var color = grid[x, y];

                if (color != PixelPigColor.None)
                {
                    counts[color]++;
                }
            }
        }

        var fallback = new List<PigSpawnData>();

        foreach (var pair in counts)
        {
            if (pair.Value > 0)
            {
                fallback.Add(new PigSpawnData(pair.Key, pair.Value));
            }
        }

        return fallback;
    }
}
