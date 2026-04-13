using System;
using System.Collections.Generic;
using System.Text;

public static class PixelFlowLevelAnalyzer
{
    private const int BoardSize = 20;
    private const int MinimumPigCount = 12;

    public static bool IsStartSolvable(PixelFlowLevelData levelData)
    {
        if (levelData == null || !HasMinimumPigCount(levelData) || !HasExactAmmoForBoard(levelData))
        {
            return false;
        }

        var state = CreateState(levelData.cells, levelData.pigLines, CreateEmptySlots(levelData.waitingSlotCount),
            CreateEmptyActivePigs(), levelData.waitingSlotCount);
        var memo = new Dictionary<string, bool>();
        return IsSolvable(state, memo);
    }

    public static bool IsCurrentStateUnlosable(
        PixelCellData[] cells,
        IReadOnlyList<IReadOnlyList<PigSpawnData>> pigLines,
        IReadOnlyList<PigSpawnData> slots,
        IReadOnlyList<PigSpawnData> activePigs,
        int slotCapacity)
    {
        var state = CreateState(cells, pigLines, slots, activePigs, slotCapacity);
        var memo = new Dictionary<string, bool>();
        return IsUnlosable(state, memo);
    }

    public static bool HasMinimumPigCount(PixelFlowLevelData levelData)
    {
        return CountPigs(levelData) >= MinimumPigCount;
    }

    public static bool HasExactAmmoForBoard(PixelFlowLevelData levelData)
    {
        var requiredByColor = BuildRequiredCounts(levelData?.cells);
        var ammoByColor = BuildAmmoCounts(levelData?.pigLines);

        foreach (var pair in requiredByColor)
        {
            if (!ammoByColor.TryGetValue(pair.Key, out var ammo) || ammo != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    public static int CountPigs(PixelFlowLevelData levelData)
    {
        return CountPigs(levelData?.pigLines);
    }

    private static bool IsSolvable(AnalysisState state, Dictionary<string, bool> memo)
    {
        if (state.RemainingCells == 0)
        {
            return true;
        }

        var key = BuildStateKey(state);

        if (memo.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (state.ActivePigs.Count > 0)
        {
            if (!TryResolveNextActivePig(state, out var nextState))
            {
                memo[key] = false;
                return false;
            }

            var result = IsSolvable(nextState, memo);
            memo[key] = result;
            return result;
        }

        var moves = BuildMoves(state);

        if (moves.Count == 0)
        {
            memo[key] = false;
            return false;
        }

        for (var i = 0; i < moves.Count; i++)
        {
            if (!TryApplyMove(state, moves[i], out var nextState))
            {
                continue;
            }

            if (IsSolvable(nextState, memo))
            {
                memo[key] = true;
                return true;
            }
        }

        memo[key] = false;
        return false;
    }

    private static bool IsUnlosable(AnalysisState state, Dictionary<string, bool> memo)
    {
        if (state.RemainingCells == 0)
        {
            return true;
        }

        var key = BuildStateKey(state);

        if (memo.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (state.ActivePigs.Count > 0)
        {
            if (!TryResolveNextActivePig(state, out var nextState))
            {
                memo[key] = false;
                return false;
            }

            var result = IsUnlosable(nextState, memo);
            memo[key] = result;
            return result;
        }

        var moves = BuildMoves(state);

        if (moves.Count == 0)
        {
            memo[key] = false;
            return false;
        }

        for (var i = 0; i < moves.Count; i++)
        {
            if (!TryApplyMove(state, moves[i], out var nextState) || !IsUnlosable(nextState, memo))
            {
                memo[key] = false;
                return false;
            }
        }

        memo[key] = true;
        return true;
    }

    private static bool TryApplyMove(AnalysisState state, AnalysisMove move, out AnalysisState nextState)
    {
        nextState = CloneState(state);

        PigSpawnData launchedPig;

        if (move.FromSlot)
        {
            launchedPig = nextState.Slots[move.Index];
            nextState.Slots[move.Index] = null;
        }
        else
        {
            launchedPig = nextState.Lines[move.Index][0];
            nextState.Lines[move.Index].RemoveAt(0);
        }

        if (launchedPig == null || launchedPig.color == PixelPigColor.None || launchedPig.ammo <= 0)
        {
            return false;
        }

        var ammoRemaining = launchedPig.ammo;
        var remainingCells = nextState.RemainingCells;
        RunLoop(nextState.Board, ref remainingCells, launchedPig.color, ref ammoRemaining);
        nextState.RemainingCells = remainingCells;

        if (ammoRemaining <= 0)
        {
            return true;
        }

        for (var i = 0; i < nextState.Slots.Count; i++)
        {
            if (nextState.Slots[i] == null)
            {
                nextState.Slots[i] = new PigSpawnData(launchedPig.color, ammoRemaining);
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveNextActivePig(AnalysisState state, out AnalysisState nextState)
    {
        nextState = CloneState(state);

        if (nextState.ActivePigs.Count == 0)
        {
            return true;
        }

        var activePig = nextState.ActivePigs[0];
        nextState.ActivePigs.RemoveAt(0);

        if (activePig == null || activePig.color == PixelPigColor.None || activePig.ammo <= 0)
        {
            return true;
        }

        var ammoRemaining = activePig.ammo;
        var remainingCells = nextState.RemainingCells;
        RunLoop(nextState.Board, ref remainingCells, activePig.color, ref ammoRemaining);
        nextState.RemainingCells = remainingCells;

        if (ammoRemaining <= 0)
        {
            return true;
        }

        for (var i = 0; i < nextState.Slots.Count; i++)
        {
            if (nextState.Slots[i] == null)
            {
                nextState.Slots[i] = new PigSpawnData(activePig.color, ammoRemaining);
                return true;
            }
        }

        return false;
    }

    private static void RunLoop(PixelPigColor[,] board, ref int remainingCells, PixelPigColor color, ref int ammoRemaining)
    {
        if (ammoRemaining <= 0)
        {
            return;
        }

        for (var x = 0; x < BoardSize && ammoRemaining > 0; x++)
        {
            TryHitColumn(board, ref remainingCells, x, BoardSize - 1, -1, color, ref ammoRemaining);
        }

        for (var y = BoardSize - 1; y >= 0 && ammoRemaining > 0; y--)
        {
            TryHitRow(board, ref remainingCells, y, BoardSize - 1, -1, color, ref ammoRemaining);
        }

        for (var x = BoardSize - 1; x >= 0 && ammoRemaining > 0; x--)
        {
            TryHitColumn(board, ref remainingCells, x, 0, 1, color, ref ammoRemaining);
        }

        for (var y = 0; y < BoardSize && ammoRemaining > 0; y++)
        {
            TryHitRow(board, ref remainingCells, y, 0, 1, color, ref ammoRemaining);
        }
    }

    private static void TryHitColumn(PixelPigColor[,] board, ref int remainingCells, int x, int startY, int step,
        PixelPigColor color, ref int ammoRemaining)
    {
        for (var y = startY; y >= 0 && y < BoardSize; y += step)
        {
            var cellColor = board[x, y];

            if (cellColor == PixelPigColor.None)
            {
                continue;
            }

            if (cellColor == color)
            {
                board[x, y] = PixelPigColor.None;
                remainingCells--;
                ammoRemaining--;
            }

            return;
        }
    }

    private static void TryHitRow(PixelPigColor[,] board, ref int remainingCells, int y, int startX, int step,
        PixelPigColor color, ref int ammoRemaining)
    {
        for (var x = startX; x >= 0 && x < BoardSize; x += step)
        {
            var cellColor = board[x, y];

            if (cellColor == PixelPigColor.None)
            {
                continue;
            }

            if (cellColor == color)
            {
                board[x, y] = PixelPigColor.None;
                remainingCells--;
                ammoRemaining--;
            }

            return;
        }
    }

    private static List<AnalysisMove> BuildMoves(AnalysisState state)
    {
        var moves = new List<AnalysisMove>();

        for (var lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
        {
            if (state.Lines[lineIndex].Count > 0)
            {
                moves.Add(new AnalysisMove(false, lineIndex));
            }
        }

        for (var slotIndex = 0; slotIndex < state.Slots.Count; slotIndex++)
        {
            if (state.Slots[slotIndex] != null)
            {
                moves.Add(new AnalysisMove(true, slotIndex));
            }
        }

        return moves;
    }

    private static AnalysisState CreateState(PixelCellData[] cells, PigLineData[] pigLines, PigSpawnData[] slots,
        PigSpawnData[] activePigs, int slotCapacity)
    {
        var lines = new List<List<PigSpawnData>>();

        if (pigLines != null)
        {
            for (var lineIndex = 0; lineIndex < pigLines.Length; lineIndex++)
            {
                var line = new List<PigSpawnData>();
                var pigs = pigLines[lineIndex] != null ? pigLines[lineIndex].pigs : null;

                if (pigs != null)
                {
                    for (var pigIndex = 0; pigIndex < pigs.Length; pigIndex++)
                    {
                        if (pigs[pigIndex] != null)
                        {
                            line.Add(new PigSpawnData(pigs[pigIndex].color, pigs[pigIndex].ammo));
                        }
                    }
                }

                lines.Add(line);
            }
        }

        return CreateState(cells, lines, slots, activePigs, slotCapacity);
    }

    private static AnalysisState CreateState(
        PixelCellData[] cells,
        IReadOnlyList<IReadOnlyList<PigSpawnData>> pigLines,
        IReadOnlyList<PigSpawnData> slots,
        IReadOnlyList<PigSpawnData> activePigs,
        int slotCapacity)
    {
        var board = new PixelPigColor[BoardSize, BoardSize];
        var remainingCells = 0;

        for (var x = 0; x < BoardSize; x++)
        {
            for (var y = 0; y < BoardSize; y++)
            {
                board[x, y] = PixelPigColor.None;
            }
        }

        if (cells != null)
        {
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];

                if (cell == null || cell.color == PixelPigColor.None)
                {
                    continue;
                }

                if (cell.x < 0 || cell.x >= BoardSize || cell.y < 0 || cell.y >= BoardSize)
                {
                    continue;
                }

                board[cell.x, cell.y] = cell.color;
                remainingCells++;
            }
        }

        var lines = new List<List<PigSpawnData>>();

        if (pigLines != null)
        {
            for (var lineIndex = 0; lineIndex < pigLines.Count; lineIndex++)
            {
                var line = new List<PigSpawnData>();
                var pigs = pigLines[lineIndex];

                if (pigs != null)
                {
                    for (var pigIndex = 0; pigIndex < pigs.Count; pigIndex++)
                    {
                        var pig = pigs[pigIndex];

                        if (pig != null)
                        {
                            line.Add(new PigSpawnData(pig.color, pig.ammo));
                        }
                    }
                }

                lines.Add(line);
            }
        }

        var slotList = new List<PigSpawnData>();

        for (var i = 0; i < slotCapacity; i++)
        {
            PigSpawnData pig = null;

            if (slots != null && i < slots.Count && slots[i] != null)
            {
                pig = new PigSpawnData(slots[i].color, slots[i].ammo);
            }

            slotList.Add(pig);
        }

        var activePigList = new List<PigSpawnData>();

        if (activePigs != null)
        {
            for (var i = 0; i < activePigs.Count; i++)
            {
                var pig = activePigs[i];

                if (pig != null)
                {
                    activePigList.Add(new PigSpawnData(pig.color, pig.ammo));
                }
            }
        }

        return new AnalysisState(board, remainingCells, lines, slotList, activePigList);
    }

    private static AnalysisState CloneState(AnalysisState state)
    {
        var board = new PixelPigColor[BoardSize, BoardSize];

        for (var x = 0; x < BoardSize; x++)
        {
            for (var y = 0; y < BoardSize; y++)
            {
                board[x, y] = state.Board[x, y];
            }
        }

        var lines = new List<List<PigSpawnData>>(state.Lines.Count);

        for (var lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
        {
            var line = new List<PigSpawnData>(state.Lines[lineIndex].Count);

            for (var pigIndex = 0; pigIndex < state.Lines[lineIndex].Count; pigIndex++)
            {
                var pig = state.Lines[lineIndex][pigIndex];
                line.Add(new PigSpawnData(pig.color, pig.ammo));
            }

            lines.Add(line);
        }

        var slots = new List<PigSpawnData>(state.Slots.Count);

        for (var i = 0; i < state.Slots.Count; i++)
        {
            var pig = state.Slots[i];
            slots.Add(pig != null ? new PigSpawnData(pig.color, pig.ammo) : null);
        }

        var activePigs = new List<PigSpawnData>(state.ActivePigs.Count);

        for (var i = 0; i < state.ActivePigs.Count; i++)
        {
            var pig = state.ActivePigs[i];
            activePigs.Add(pig != null ? new PigSpawnData(pig.color, pig.ammo) : null);
        }

        return new AnalysisState(board, state.RemainingCells, lines, slots, activePigs);
    }

    private static string BuildStateKey(AnalysisState state)
    {
        var builder = new StringBuilder(BoardSize * BoardSize + 128);

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                builder.Append((char)('A' + (int)state.Board[x, y] + 1));
            }
        }

        builder.Append('|');

        for (var lineIndex = 0; lineIndex < state.Lines.Count; lineIndex++)
        {
            for (var pigIndex = 0; pigIndex < state.Lines[lineIndex].Count; pigIndex++)
            {
                var pig = state.Lines[lineIndex][pigIndex];
                builder.Append((int)pig.color).Append(':').Append(pig.ammo).Append(',');
            }

            builder.Append(';');
        }

        builder.Append('|');

        var slotKeys = new List<string>(state.Slots.Count);

        for (var i = 0; i < state.Slots.Count; i++)
        {
            var pig = state.Slots[i];
            slotKeys.Add(pig == null ? "_" : $"{(int)pig.color}:{pig.ammo}");
        }

        slotKeys.Sort(StringComparer.Ordinal);

        for (var i = 0; i < slotKeys.Count; i++)
        {
            builder.Append(slotKeys[i]).Append(';');
        }

        builder.Append('|');

        for (var i = 0; i < state.ActivePigs.Count; i++)
        {
            var pig = state.ActivePigs[i];
            builder.Append(pig == null ? "_" : $"{(int)pig.color}:{pig.ammo}").Append(';');
        }

        return builder.ToString();
    }

    private static PigSpawnData[] CreateEmptySlots(int slotCount)
    {
        var safeSlotCount = Math.Max(0, slotCount);
        return new PigSpawnData[safeSlotCount];
    }

    private static PigSpawnData[] CreateEmptyActivePigs()
    {
        return Array.Empty<PigSpawnData>();
    }

    private static Dictionary<PixelPigColor, int> BuildRequiredCounts(PixelCellData[] cells)
    {
        var counts = CreateColorCountMap();

        if (cells == null)
        {
            return counts;
        }

        for (var i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];

            if (cell != null && cell.color != PixelPigColor.None)
            {
                counts[cell.color]++;
            }
        }

        return counts;
    }

    private static Dictionary<PixelPigColor, int> BuildAmmoCounts(PigLineData[] pigLines)
    {
        var counts = CreateColorCountMap();

        if (pigLines == null)
        {
            return counts;
        }

        for (var lineIndex = 0; lineIndex < pigLines.Length; lineIndex++)
        {
            var pigs = pigLines[lineIndex] != null ? pigLines[lineIndex].pigs : null;

            if (pigs == null)
            {
                continue;
            }

            for (var pigIndex = 0; pigIndex < pigs.Length; pigIndex++)
            {
                var pig = pigs[pigIndex];

                if (pig != null && pig.color != PixelPigColor.None && pig.ammo > 0)
                {
                    counts[pig.color] += pig.ammo;
                }
            }
        }

        return counts;
    }

    private static int CountPigs(PigLineData[] pigLines)
    {
        var pigCount = 0;

        if (pigLines == null)
        {
            return 0;
        }

        for (var lineIndex = 0; lineIndex < pigLines.Length; lineIndex++)
        {
            var pigs = pigLines[lineIndex] != null ? pigLines[lineIndex].pigs : null;

            if (pigs == null)
            {
                continue;
            }

            for (var pigIndex = 0; pigIndex < pigs.Length; pigIndex++)
            {
                var pig = pigs[pigIndex];

                if (pig != null && pig.color != PixelPigColor.None && pig.ammo > 0)
                {
                    pigCount++;
                }
            }
        }

        return pigCount;
    }

    private static Dictionary<PixelPigColor, int> CreateColorCountMap()
    {
        var counts = new Dictionary<PixelPigColor, int>();

        foreach (PixelPigColor color in Enum.GetValues(typeof(PixelPigColor)))
        {
            if (color != PixelPigColor.None)
            {
                counts[color] = 0;
            }
        }

        return counts;
    }

    private readonly struct AnalysisMove
    {
        public AnalysisMove(bool fromSlot, int index)
        {
            FromSlot = fromSlot;
            Index = index;
        }

        public bool FromSlot { get; }
        public int Index { get; }
    }

    private sealed class AnalysisState
    {
        public AnalysisState(PixelPigColor[,] board, int remainingCells, List<List<PigSpawnData>> lines, List<PigSpawnData> slots,
            List<PigSpawnData> activePigs)
        {
            Board = board;
            RemainingCells = remainingCells;
            Lines = lines;
            Slots = slots;
            ActivePigs = activePigs;
        }

        public PixelPigColor[,] Board { get; }
        public int RemainingCells { get; set; }
        public List<List<PigSpawnData>> Lines { get; }
        public List<PigSpawnData> Slots { get; }
        public List<PigSpawnData> ActivePigs { get; }
    }
}
