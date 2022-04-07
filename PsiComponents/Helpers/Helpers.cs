using MathNet.Spatial.Euclidean;
using MathNet.Numerics.LinearAlgebra;
using System.IO;

namespace Helpers
{
    public class Helpers
    {
        static public T CantorPairing<T>(ref T k1, ref T k2)
        {
#pragma warning disable CS8600 // Conversion de littéral ayant une valeur null ou d'une éventuelle valeur null en type non-nullable.
            return 0.5 * ((dynamic)k1 + (dynamic)k2) * ((dynamic)k1 + (dynamic)k2 + 1) + (dynamic)k2;
#pragma warning restore CS8600 // Conversion de littéral ayant une valeur null ou d'une éventuelle valeur null en type non-nullable.
        }

        static public T CantorParingSequence<T>(ref List<T> set)
        {
            T value = set.ElementAt(0);
            for (int iterator = 1; iterator < set.Count(); iterator++)
            { 
                T value2 = set[iterator];
                value = CantorPairing(ref value, ref value2);
            }
            return value;
        }

        static public Vector3D NuitrackToMathNet(nuitrack.Vector3 vect)
        {
            return new Vector3D(vect.X, vect.Y, vect.Z);
        }

        static public Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel FloatToConfidence(float confidence)
        {
            if(confidence == 0f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.None;
            if (confidence < 0.33f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Low;
            if (confidence < 0.66f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium;
            return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.High;
        }
        static public Vector3D CalculateTransform(Vector3D origin, Matrix<double> transformationMatrix)
        {
            Vector<double> v4Origin = Vector<double>.Build.Dense(4);
            v4Origin[0] = origin.X;
            v4Origin[1] = origin.Y;
            v4Origin[2] = origin.Z;
            v4Origin[3] = 1.0f;
            var result = v4Origin * transformationMatrix;
            return new Vector3D(result[0], result[1], result[2]);
        }

        static public bool ReadCalibrationFromFile(string filepath, out Matrix<double> matrix)
        {
            matrix = Matrix<double>.Build.DenseIdentity(4, 4);
            var matrixStr = File.ReadLines(filepath);
            int count = 0;
            double[,] valuesD = new double[4,4];
            foreach (string line in matrixStr)
            {
                foreach (string value in line.Split(' '))
                {
                    if (value.Length == 0)
                        continue;
                    valuesD[count/4, count%4] = Double.Parse(value);
                    count++;
                }
            }
            matrix = Matrix<double>.Build.DenseOfArray(valuesD);
            return true;
        }

        public static bool IsValidDouble(double val)
        {
            if (Double.IsNaN(val))
            {
                return false;
            }
            if (Double.IsInfinity(val))
            {
                return false;
            }
            return true;
        }

        public static bool IsValidPoint2D(Point2D point) => IsValidDouble(point.X) && IsValidDouble(point.Y);

    }
}
