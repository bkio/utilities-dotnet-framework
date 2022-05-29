/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Grid3 : Shape
    {
        public const string NODE_SIZE_PROPERTY = "nodeSize";
        public const string CELL_SIZE_PROPERTY = "cellSize";

        [JsonProperty(NODE_SIZE_PROPERTY)]
        private Index3 NodeSizeValue;

        [JsonProperty(CELL_SIZE_PROPERTY)]
        private Index3 CellSizeValue;
        private Index3 MaxNodeValue;
        private Index3 MaxCellValue;
        private int NodeSizeValueIJ;
        private int CellSizeValueIJ;
        protected double MaxNodeValue_IPlusEpsilon;
        protected double MaxNodeValue_JPlusEpsilon;
        protected double MaxNodeValue_KPlusEpsilon;
        protected double MaxNodeValue_IMinusEpsilon;
        protected double MaxNodeValue_JMinusEpsilon;
        protected double MaxNodeValue_KMinusEpsilon;

        protected static readonly double PlusEpsilon = 0.00001;
        protected static readonly double MinusEpsilon = -0.00001;

        public Grid3(Index3 _NodeSize) { NodeSize = _NodeSize; }
        public Grid3(Grid3 _Source) : base(_Source) { NodeSize = _Source.NodeSize; }

        public Index3 NodeSize
        {
            get => NodeSizeValue;
            set
            {
                NodeSizeValue = value;
                CellSizeValue = NodeSizeValue - Index3.Unit;
                CellSizeValue.SetMax(Index3.Zero);
                MaxNodeValue = NodeSizeValue - Index3.Unit;
                MaxCellValue = CellSizeValue - Index3.Unit;
                NodeSizeValueIJ = NodeSizeValue.I * NodeSizeValue.J;
                CellSizeValueIJ = CellSizeValue.I * CellSizeValue.J;

                MaxNodeValue_IPlusEpsilon = MaxNodeValue.I + PlusEpsilon;
                MaxNodeValue_JPlusEpsilon = MaxNodeValue.J + PlusEpsilon;
                MaxNodeValue_KPlusEpsilon = MaxNodeValue.K + PlusEpsilon;
                MaxNodeValue_IMinusEpsilon = MaxNodeValue.I - PlusEpsilon;
                MaxNodeValue_JMinusEpsilon = MaxNodeValue.J - PlusEpsilon;
                MaxNodeValue_KMinusEpsilon = MaxNodeValue.K - PlusEpsilon;
            }
        }

        [JsonProperty(NODE_SIZE_PROPERTY)]
        public Index3 CellSize
        {
            get => CellSizeValue;
            set => NodeSize = value + Index3.Unit;
        }

        public int NumNodes => NodeSizeValue.Size;
        public int NumCells => CellSizeValue.Size;

        public int NumNodesI => NodeSizeValue.I;
        public int NumNodesJ => NodeSizeValue.J;
        public int NumNodesK => NodeSizeValue.K;

        public int MaxNodeI => MaxNodeValue.I;
        public int MaxNodeJ => MaxNodeValue.J;
        public int MaxNodeK => MaxNodeValue.K;

        public int NumCellsI => CellSizeValue.I;
        public int NumCellsJ => CellSizeValue.J;
        public int NumCellsK => CellSizeValue.K;

        public int MaxCellI => MaxCellValue.I;
        public int MaxCellJ => MaxCellValue.J;
        public int MaxCellK => MaxCellValue.K;

        public override string ToString() { return $"Nodes={NodeSize} Cells={CellSize}"; }

        public Index3 GetNodeFromNodeIndex(int _NodeIndex)
        {
            var K = _NodeIndex / NodeSizeValueIJ;
            _NodeIndex %= NodeSizeValueIJ;
            return new Index3(_NodeIndex % NodeSizeValue.I, _NodeIndex / NodeSizeValue.I, K);
        }

        public Index3 GetCellFromCellIndex(int _CellIndex)
        {
            var K = _CellIndex / CellSizeValueIJ;
            _CellIndex %= CellSizeValueIJ;
            return new Index3(_CellIndex % CellSizeValue.I, _CellIndex / CellSizeValue.I, K);
        }

        public Index3 GetCornerNode(int _Corner)
        {
            switch (_Corner)
            {
                case 0:
                    return Index3.Zero;
                case 1:
                    return new Index3(MaxNodeI, 0, 0);
                case 2:
                    return new Index3(MaxNodeI, MaxNodeJ, 0);
                case 3:
                    return new Index3(0, MaxNodeJ, 0);
                case 4:
                    return new Index3(0, 0, MaxNodeK);
                case 5:
                    return new Index3(MaxNodeI, 0, MaxNodeK);
                case 6:
                    return new Index3(MaxNodeI, MaxNodeJ, MaxNodeK);
                case 7:
                    return new Index3(0, MaxNodeJ, MaxNodeK);
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public bool IsNodeInside(Index3 _Node) { return _Node.I >= 0 && _Node.J >= 0 && _Node.K >= 0 && _Node.I <= MaxNodeValue.I && _Node.J <= MaxNodeValue.J && _Node.K <= MaxNodeValue.K; }

        public bool IsCellInside(Vector3 _Cell) { return _Cell.X >= 0 && _Cell.Y >= 0 && _Cell.Z >= 0 && _Cell.X < MaxNodeValue.I && _Cell.Y < MaxNodeValue.J && _Cell.Z < MaxNodeValue.K; }

        public bool HasSameSize(Grid3 _Value) { return NodeSizeValue == _Value.NodeSizeValue; }

        public void ForceCellInside(ref Index3 _Cell)
        {
            TruncateCellByMin(ref _Cell);
            TruncateCellByMax(ref _Cell);
        }

        public void TruncateCellByMin(ref Index3 _Cell)
        {
            if (_Cell.I < 0)
                _Cell.I = 0;
            if (_Cell.J < 0)
                _Cell.J = 0;
            if (_Cell.K < 0)
                _Cell.K = 0;
        }

        public void TruncateCellByMax(ref Index3 _Cell)
        {
            if (_Cell.I > MaxCellValue.I)
                _Cell.I = MaxCellValue.I;
            if (_Cell.J > MaxCellValue.J)
                _Cell.J = MaxCellValue.J;
            if (_Cell.K > MaxCellValue.K)
                _Cell.K = MaxCellValue.K;
        }

        public T[,,] CreateCellBuffer<T>()
        { 
            return new T[NumCellsI, NumCellsJ, NumCellsK];
        }
    }
}