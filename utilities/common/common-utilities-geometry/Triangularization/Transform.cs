/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public class Transform
    {
        public double Location_X = 0.0f;
        public double Location_Y = 0.0f;
        public double Location_Z = 0.0f;

        public double Rotation_Pitch = 0.0f;
        public double Rotation_Yaw = 0.0f;
        public double Rotation_Roll = 0.0f;

        public double Scale_X = 1.0f;
        public double Scale_Y = 1.0f;
        public double Scale_Z = 1.0f;

        public static readonly int CompileWith_UniqueID_ItemColor_Size = 100;

        public static Transform CalculateRelativeTransform(double[] _Matrix)
        {
            if (_Matrix != null)
            {
                Matrix3x4 LocalToWorldMatrix = new Matrix3x4(
                    _Matrix[0], _Matrix[4], _Matrix[8], _Matrix[12],
                    _Matrix[1], _Matrix[5], _Matrix[9], _Matrix[13],
                    _Matrix[2], _Matrix[6], _Matrix[10], _Matrix[14]);

                Vector3 RelativeRotation = CalculateRotation(_Matrix);

                Vector3 Scale = CalculateScale(LocalToWorldMatrix);

                Transform Result = new Transform
                {
                    Location_X = LocalToWorldMatrix.X,
                    Location_Y = LocalToWorldMatrix.Y,
                    Location_Z = LocalToWorldMatrix.Z,

                    Rotation_Pitch = RelativeRotation.X,
                    Rotation_Yaw = RelativeRotation.Y,
                    Rotation_Roll = RelativeRotation.Z,

                    Scale_X = Scale.X,
                    Scale_Y = Scale.Y,
                    Scale_Z = Scale.Z
                };

                return Result;
            }
            return null;
        }

        private static Vector3 CalculateScale(Matrix3x4 _Matrix)
        {
            var x = Math.Sqrt(_Matrix.M11 * _Matrix.M11 + _Matrix.M21 * _Matrix.M21 + _Matrix.M31 * _Matrix.M31);
            var y = Math.Sqrt(_Matrix.M12 * _Matrix.M12 + _Matrix.M22 * _Matrix.M22 + _Matrix.M32 * _Matrix.M31);
            var z = Math.Sqrt(_Matrix.M13 * _Matrix.M13 + _Matrix.M23 * _Matrix.M23 + _Matrix.M33 * _Matrix.M33);

            return new Vector3(x, y, z);
        }

        //Transform matrix to Euler rotation
        private static Vector3 CalculateRotation(double[] _LocalToWorldMatrix)
        {
            double Pitch, Yaw, Roll;

            if (_LocalToWorldMatrix[0] == 1.0f)
            {
                Yaw = Math.Atan2(_LocalToWorldMatrix[8], _LocalToWorldMatrix[14]);
                Pitch = 0;
                Roll = 0;
            }
            else if (_LocalToWorldMatrix[0] == -1.0f)
            {
                Yaw = Math.Atan2(_LocalToWorldMatrix[8], _LocalToWorldMatrix[14]);
                Pitch = 0;
                Roll = 0;
            }
            else
            {
                Yaw = Math.Atan2(-_LocalToWorldMatrix[3], _LocalToWorldMatrix[0]);
                Pitch = Math.Asin(_LocalToWorldMatrix[2]);
                Roll = Math.Atan2(-_LocalToWorldMatrix[9], _LocalToWorldMatrix[5]);
            }
            return new Vector3(Pitch, Yaw, Roll);
        }

        private static Vector3 CalculateRotation(Matrix3x4 _LocalToWorldMatrix)
        {
            double Pitch, Yaw, Roll;

            if (_LocalToWorldMatrix.M11 == 1.0f)
            {
                Yaw = Math.Atan2(_LocalToWorldMatrix.M13, _LocalToWorldMatrix.Z);
                Pitch = 0;
                Roll = 0;
            }
            else if (_LocalToWorldMatrix.M11 == -1.0f)
            {
                Yaw = Math.Atan2(_LocalToWorldMatrix.M13, _LocalToWorldMatrix.Z);
                Pitch = 0;
                Roll = 0;
            }
            else
            {
                Yaw = Math.Atan2(-_LocalToWorldMatrix.M31, _LocalToWorldMatrix.M11);
                Pitch = Math.Asin(_LocalToWorldMatrix.M21);
                Roll = Math.Atan2(-_LocalToWorldMatrix.M23, _LocalToWorldMatrix.M22);
            }
            return new Vector3(Pitch, Yaw, Roll);
        }

        private static Vector3 CalculateRotationPixyz(Matrix3x4 _Matrix)
        {
            var Sy = Math.Sqrt(_Matrix.M11 * _Matrix.M11 + _Matrix.M21 * _Matrix.M21);

            var Singular = Sy < 1e-6;

            double X, Y, Z;

            if (Singular)
            {
                X = Math.Atan2(-_Matrix.M23, _Matrix.M22);
                Y = Math.Atan2(-_Matrix.M31, Sy);
                Z = 0.0f;
            }
            else
            {
                X = Math.Atan2(_Matrix.M32, _Matrix.M33);
                Y = Math.Atan2(_Matrix.M31, Sy);
                Z = Math.Atan2(_Matrix.M21, _Matrix.M11);
            }

            return new Vector3(X, Y, Z);
        }

        public byte[] CompileWith_UniqueID_ItemColor(uint _UniqueID, Vector3 _ItemColor)
        {
            /*
             * uint UniqueID                                        1 uint                      * 4         = 4
             * Vector3 RelativeLocation                             3 doubles                    * 8         = 24
             * Vector3 RelativeRotation (Pitch, Yaw, Roll)          3 doubles                    * 8         = 24
             * Vector3 Scale                                        3 doubles                    * 8         = 24 
             * Vector3 ItemColor                                    3 double                    * 8         = 24
             *                                                                                        Total = (CompileWith_UniqueID_ItemColor_Size)
             */

            var Result = new byte[CompileWith_UniqueID_ItemColor_Size];

            //uint UniqueID
            Array.Copy(BitConverter.GetBytes(_UniqueID), 0, Result, 0, 4);

            //Vector3 RelativeLocation
            Array.Copy(BitConverter.GetBytes(Location_X), 0, Result, 4, 8);
            Array.Copy(BitConverter.GetBytes(Location_Y), 0, Result, 12, 8);
            Array.Copy(BitConverter.GetBytes(Location_Z), 0, Result, 20, 8);

            //Vector3 RelativeRotation
            Array.Copy(BitConverter.GetBytes(Rotation_Pitch), 0, Result, 28, 8);
            Array.Copy(BitConverter.GetBytes(Rotation_Yaw), 0, Result, 36, 8);
            Array.Copy(BitConverter.GetBytes(Rotation_Roll), 0, Result, 44, 8);

            //Vector3 Scale
            Array.Copy(BitConverter.GetBytes(Scale_X), 0, Result, 52, 8);
            Array.Copy(BitConverter.GetBytes(Scale_Y), 0, Result, 60, 8);
            Array.Copy(BitConverter.GetBytes(Scale_Z), 0, Result, 68, 8);

            //Vector3 ItemColor
            Array.Copy(BitConverter.GetBytes(_ItemColor.X), 0, Result, 76, 8);
            Array.Copy(BitConverter.GetBytes(_ItemColor.Y), 0, Result, 84, 8);
            Array.Copy(BitConverter.GetBytes(_ItemColor.Z), 0, Result, 92, 8);

            return Result;
        }

        public static Tuple<Transform, uint, Vector3> DecompileTo_Transform_UniqueID_ItemColor(byte[] _CompiledData)
        {
            if (_CompiledData != null && _CompiledData.Length >= CompileWith_UniqueID_ItemColor_Size)
            {
                var Transform = new Transform();

                //uint UniqueID
                uint UniqueID = BitConverter.ToUInt32(_CompiledData, 0);

                //Vector3 RelativeLocation
                Transform.Location_X = BitConverter.ToDouble(_CompiledData, 4);
                Transform.Location_Y = BitConverter.ToDouble(_CompiledData, 12);
                Transform.Location_Z = BitConverter.ToDouble(_CompiledData, 20);

                //Vector3 RelativeRotation
                Transform.Rotation_Pitch = BitConverter.ToDouble(_CompiledData, 28);
                Transform.Rotation_Yaw = BitConverter.ToDouble(_CompiledData, 36);
                Transform.Rotation_Roll = BitConverter.ToDouble(_CompiledData, 44);

                //Vector3 Scale
                Transform.Scale_X = BitConverter.ToDouble(_CompiledData, 52);
                Transform.Scale_Y = BitConverter.ToDouble(_CompiledData, 60);
                Transform.Scale_Z = BitConverter.ToDouble(_CompiledData, 68);

                //Vector3 ItemColor
                var ItemColor = new Vector3
                {
                    X = BitConverter.ToDouble(_CompiledData, 76),
                    Y = BitConverter.ToDouble(_CompiledData, 84),
                    Z = BitConverter.ToDouble(_CompiledData, 92)
                };

                return new Tuple<Transform, uint, Vector3>(Transform, UniqueID, ItemColor);
            }
            return null;
        }
    }
}