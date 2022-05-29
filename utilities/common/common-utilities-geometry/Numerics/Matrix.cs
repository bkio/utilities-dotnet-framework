/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Numerics
{
    public abstract class Matrix
    {
        public int NumRows { get; protected set; }
        public int NumCols { get; protected set; }
        public virtual double GetAt(int _Row, int _Col) { throw new NotImplementedException(); }

        public override bool Equals(object _Object)
        {
            return IsEqual((Matrix)_Object);
        }

        public override int GetHashCode() { return NumRows + NumCols; }

        public override string ToString()
        {
            var Text = $"{GetType().Name}{": (" + NumRows + "x" + NumCols})\n";
            for (var Row = 0; Row < NumRows; Row++)
            {
                Text += "| ";
                for (var Col = 0; Col < NumCols; Col++)
                    Text += GetString(GetAt(Row, Col));
                Text += " |\n";
            }
            return Text;
        }

        public virtual bool IsEqual(Matrix _other)
        {
            if (ReferenceEquals(this, _other))
                return true;

            if (ReferenceEquals(_other, null))
                return false;

            if (!IsEqualDimensions(_other))
                return false;

            for (var i = 0; i < NumRows; i++)
                for (var j = 0; j < NumCols; j++)
                    if (!GetAt(i, j).IsRelEqual(_other.GetAt(i, j)))
                        return false;

            return true;
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual Matrix Add(Matrix _Other)
        {
            throw new NotImplementedException();
        }

        public virtual Matrix Subtract(Matrix _Other)
        {
            throw new NotImplementedException();
        }

        public virtual Matrix Multiply(Matrix _Other)
        {
            throw new NotImplementedException();
        }

        public virtual void Multiply(double _Other)
        {
            throw new NotImplementedException();
        }

        public virtual void Multiply(double[] _Other, double[] _Result)
        {
            throw new NotImplementedException();
        }

        public virtual void MultiplyTranspose(double[] _Other, double[] _Result)
        {
            throw new NotImplementedException();
        }

        public virtual Matrix GetTransposedBase()
        {
            throw new NotImplementedException();
        }

        public virtual Range1 GetPreudoSolutionRange(double[] _B)
        {
            throw new NotImplementedException();
        }

        public bool IsEqualDimensions(Matrix _Other)
        {
            if (_Other == null)
                return false;

            if (NumRows != _Other.NumRows || NumCols != _Other.NumCols)
                return false;

            return true;
        }

        public static bool Equals(Matrix _A, Matrix _B)
        {
            if (ReferenceEquals(_A, _B))
                return true;

            if (ReferenceEquals(_A, null))
                return false;

            if (ReferenceEquals(_A, null))
                return false;

            return _A.IsEqual(_B);
        }

        public static string GetString(double _Value, int _Decimals = 4)
        {
            return $"{_Value.ToString("F" + _Decimals).PadLeft(_Decimals + 4)} ";
        }

        public static string GetString(double _M1, double _M2, double _M3, double _M4, int _Decimals = 4)
        {
            return $"|{GetString(_M1, _Decimals)}, {GetString(_M2, _Decimals)}, {GetString(_M3, _Decimals)}, {GetString(_M4, _Decimals)}|\n";
        }
    }
}