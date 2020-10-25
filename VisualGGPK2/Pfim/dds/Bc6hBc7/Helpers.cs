using System;
using System.Diagnostics;

namespace Pfim.dds.Bc6hBc7
{
    internal static class Helpers
    {
        public static bool IsFixUpOffset(byte uPartitions, byte uShape, int uOffset)
        {
            Debug.Assert(uPartitions < 3 && uShape < 64 && uOffset < 16 && uOffset >= 0);
            for (byte p = 0; p <= uPartitions; p++)
            {
                if (uOffset == Constants.g_aFixUp[uPartitions][uShape][p])
                {
                    return true;
                }
            }
            return false;
        }

        public static void TransformInverse(INTEndPntPair[] aEndPts, LDRColorA Prec, bool bSigned)
        {
            INTColor WrapMask = new INTColor((1 << Prec.r) - 1, (1 << Prec.g) - 1, (1 << Prec.b) - 1);
            aEndPts[0].B += aEndPts[0].A; aEndPts[0].B &= WrapMask;
            aEndPts[1].A += aEndPts[0].A; aEndPts[1].A &= WrapMask;
            aEndPts[1].B += aEndPts[0].A; aEndPts[1].B &= WrapMask;
            if (bSigned)
            {
                aEndPts[0].B.SignExtend(Prec);
                aEndPts[1].A.SignExtend(Prec);
                aEndPts[1].B.SignExtend(Prec);
            }
        }

        // Fill colors where each pixel is 4 bytes (rgba)
        public static void FillWithErrorColors(byte[] pOut, ref uint index, int numPixels, byte divSize, uint stride)
        {
            int rem;
            for (int i = 0; i < numPixels; ++i)
            {
#if DEBUG
                // Use Magenta in debug as a highly-visible error color
                pOut[index++] = 255;
                pOut[index++] = 0;
                pOut[index++] = 255;
                pOut[index++] = 255;
#else
                // In production use, default to black
                pOut[index++] = 0;
                pOut[index++] = 0;
                pOut[index++] = 0;
                pOut[index++] = 255;
#endif

                Math.DivRem(i + 1, divSize, out rem);
                if (rem == 0)
                    index += 4 * (stride - divSize);
            }
        }
    }
}
