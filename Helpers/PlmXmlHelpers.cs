using System.Globalization;
using System.Numerics;

namespace PlmXmlSyntax.Helpers;

public static class PlmXmlHelpers
{
    public static Matrix4x4 ParseTransform(this string transformString)
    {
        if (string.IsNullOrWhiteSpace(transformString))
            return Matrix4x4.Identity;

        var parts = transformString.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 16)
            return Matrix4x4.Identity;

        float[] values = new float[16];
        for (int i = 0; i < 16; i++)
        {
            if (float.TryParse(parts[i], CultureInfo.InvariantCulture, out float val))
            {
                values[i] = val;
            }
        }

        // Matrix4x4 constructor takes M11, M12, ... order
        return new Matrix4x4(
            values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11],
            values[12], values[13], values[14], values[15]
        );
    }

    public static string ToExplicitDecimalString(this Matrix4x4 matrix)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}",
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }
}
