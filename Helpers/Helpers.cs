using System.Numerics;
using nuitrack;

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

        static public System.Numerics.Vector3 NuitrackToSystem(nuitrack.Vector3 vect)
        {
            return new System.Numerics.Vector3(vect.X, vect.X, vect.Z);
        }

        static public System.Numerics.Vector3 AzureToSystem(MathNet.Spatial.Euclidean.Point3D vect)
        {
            return new System.Numerics.Vector3((float)vect.X, (float)vect.X, (float)vect.Z);
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

    }
}
