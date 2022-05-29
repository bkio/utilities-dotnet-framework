/// Copyright 2022- Burak Kara, All rights reserved.

namespace CommonUtilities.Geometry.Triangularization
{
    //
    //          Type0      Type1       Type2     Type3      Type01     Type23  
    //                2   3------2    3         3------2   3------2   3------2 
    //              / |   |     /     | \        \     |   |     /|   | \    | 
    //            /   |   |   /       |   \        \   |   |   /  |   |   \  |  
    //          /     |   | /         |     \        \ |   | /    |   |     \| 
    //         0------1   0           0------1         1   0------1   0------1 
    //

    public enum ETriangleType : byte
    {
        Type0 = 0,
        Type1 = 1,
        Type2 = 2,
        Type3 = 3,
        Type01 = 4, // From upper left to lower right.  
        Type23 = 5, // From upper right to lower left.  
        None = 6
    }
}