using System.Runtime.InteropServices;

namespace SCOI_3
{

    class BinaryCPP
    {
        [DllImport("SCOIDLL.dll", EntryPoint = "ToGray", CallingConvention = CallingConvention.StdCall)]
        static public extern double ToGray(byte[] bytes1, int length, int BitPerPixel, ref byte minPixel);

        [DllImport("SCOIDLL.dll", EntryPoint = "Gavrilov", CallingConvention = CallingConvention.StdCall)]
        static public extern double Gavrilov(byte[] bytes1, byte[] bClone, int length, int width, int height, int BitPerPixel);

        [DllImport("SCOIDLL.dll", EntryPoint = "Otsu", CallingConvention = CallingConvention.StdCall)]
        static public extern double Otsu(byte[] bytes1, byte[] bClone, int length, int width, int height, int BitPerPixel);

        [DllImport("SCOIDLL.dll", EntryPoint = "CalcIntegralMatrix", CallingConvention = CallingConvention.StdCall)]
        static public extern void CalcIntegralMatrix(byte[] bytes, int width, int height, int BitPerPixel, ulong[] Out);

        [DllImport("SCOIDLL.dll", EntryPoint = "CalcIntegralMatrix2", CallingConvention = CallingConvention.StdCall)]
        static public extern void CalcIntegralMatrix2(byte[] bytes, int width, int height, int BitPerPixel, ulong[] Out, ulong[] Out2);

        [DllImport("SCOIDLL.dll", EntryPoint = "CalcIntegralSqrMatrix", CallingConvention = CallingConvention.StdCall)]
        static public extern void CalcIntegralSqrMatrix(byte[] bytes, int width, int height, int BitPerPixel, ulong[] Out);

        [DllImport("SCOIDLL.dll", EntryPoint = "GetDispersion", CallingConvention = CallingConvention.StdCall)]
        static public extern double GetDispersion(ulong[] integralMat, ulong[] integralMatSqr, int height, int width, int BitPerPixel, int indexByte, int rect, ref double M);

        [DllImport("SCOIDLL.dll", EntryPoint = "Niblek", CallingConvention = CallingConvention.StdCall)]
        static public extern double Niblek(byte[] bytes1, byte[] bClone, int length, ulong[] integralMat, ulong[] integralMatSqr, int width, int height, int BitPerPixel, int a, double k);

        [DllImport("SCOIDLL.dll", EntryPoint = "NiblekMultiThreading", CallingConvention = CallingConvention.StdCall)]
        static public extern double NiblekMultiThreading(byte[] bytes1, byte[] bClone, int length, ulong[] integralMat, ulong[] integralMatSqr, int width, int height, int BitPerPixel, int a, double k);

        [DllImport("SCOIDLL.dll", EntryPoint = "Sauvola", CallingConvention = CallingConvention.StdCall)]
        static public extern double Sauvola(byte[] bytes1, byte[] bClone, int length, ulong[] integralMat, ulong[] integralMatSqr, int width, int height, int BitPerPixel, int a, double k);

        [DllImport("SCOIDLL.dll", EntryPoint = "KristianWolf", CallingConvention = CallingConvention.StdCall)]
        static public extern double KristianWolf(byte[] bytes1, byte[] bClone, int length, ulong[] integralMat, ulong[] integralMatSqr, int width, int height, int BitPerPixel, int a, double k, byte minPixel);

        [DllImport("SCOIDLL.dll", EntryPoint = "Bradley", CallingConvention = CallingConvention.StdCall)]
        static public extern double Bradley(byte[] bytes1, byte[] bClone, int length, ulong[] integralMat, int width, int height, int BitPerPixel, int a, double k);
    }

}
