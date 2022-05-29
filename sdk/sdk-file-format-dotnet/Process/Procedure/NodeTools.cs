/// Copyright 2022- Burak Kara, All rights reserved.

using SDKFileFormat.Process.RandomAccessFile;

namespace SDKFileFormat.Process.Procedure
{
    public static class NodeTools
    {
        public static void UniqueIDToStartIndexAndSize(ulong _UniqueID, out uint _StartIndex, out uint _Size)
        {
            _StartIndex = (uint)(_UniqueID >> 32);
            _Size = (uint)(_UniqueID & 0x00000000FFFFFFFF);
        }

        public static int BufferToNode(out Node _Result, ENodeType _NodeType, byte[] _Buffer, int _Offset = 0)
        {
            _Result = null;

            switch (_NodeType)
            {
                case ENodeType.Hierarchy:
                    _Result = new HierarchyNode();
                    break;
                case ENodeType.Geometry:
                    _Result = new GeometryNode();
                    break;
                case ENodeType.Metadata:
                    _Result = new MetadataNode();
                    break;
            }

            return _Result.FromBytes(_Buffer, _Offset);
        }
    }
}