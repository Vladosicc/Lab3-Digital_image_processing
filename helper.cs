using System.Collections.Generic;

namespace SCOI_3
{
    public static class _helper
    {
        public static double Sum(this double[] arr, int finallyIndex = 255)
        {
            double res = 0;
            for (int i = 0; i < finallyIndex; i++)
            {
                res += arr[i];
            }
            return res;
        }

        public static double SumMult(this double[] arr, int finallyIndex = 255)
        {
            double res = 0;
            for (int i = 0; i < finallyIndex; i++)
            {
                res += arr[i] * i;
            }
            return res;
        }

        public static byte[][] To2xArray(this byte[] arr, int height, int width, int bitPerPix)
        {
            byte[][] res = new byte[width][];
            List<int> ad = new List<int>();
            for (int i = 0; i < width; i++)
            {
                res[i] = new byte[height];
                for (int j = 0; j < height * 4; j += 4)
                {
                    res[i][j / 4] = arr[j * width + i * (bitPerPix / 8)];
                    ad.Add(j * width + i * (bitPerPix / 8));
                }
            }
            return res;
        }

        public static byte[][] To2xArray2(this byte[] arr, int height, int width, int bitPerPix)
        {
            int bInPix = (bitPerPix / 8);
            byte[][] res = new byte[height][];
            for (int i = 0; i < height; i++)
            {
                res[i] = new byte[width];
                for (int j = 0; j < width; j++)
                {
                    res[i][j] = arr[i * width * bInPix + j * bInPix];
                }
            }
            return res;
        }

        //Байты идут по столбцам
        public static double SumMatrix(byte[][] arr, int indexByte, int height, int width, int a, int BitsPerPix)
        {
            int sum = 0;
            int rowIndex = (indexByte / (BitsPerPix / 8)) / width;
            int colIndex = (indexByte / (BitsPerPix / 8)) % width;

            int rowNow = rowIndex - a / 2;
            int colNow = colIndex - a / 2;

            int countPixInA = 0;

            for (int i = 0; i < a; i++)
            {
                if (colNow < 0)
                {
                    colNow++;
                    continue;
                }
                if (colNow >= width)
                {
                    colNow--;
                    continue;
                }
                rowNow = rowIndex - a / 2;
                for (int j = 0; j < a; j++)
                {
                    if (rowNow < 0)
                    {
                        rowNow++;
                        continue;
                    }
                    if (rowNow >= height)
                    {
                        rowNow--;
                        continue;
                    }
                    sum += arr[rowNow][colNow];
                    rowNow++;
                    countPixInA++;
                }
                colNow++;
            }
            return sum / (double)countPixInA;
        }

        public static double SumMatrixSqr(byte[][] arr, int indexByte, int height, int width, int a, int BitsPerPix)
        {
            int sum = 0;
            int rowIndex = (indexByte / (BitsPerPix / 8)) / width;
            int colIndex = (indexByte / (BitsPerPix / 8)) % width;

            int rowNow = rowIndex - a / 2;
            int colNow = colIndex - a / 2;

            int countPixInA = 0;

            for (int i = 0; i < a; i++)
            {
                if (colNow < 0)
                {
                    colNow++;
                    continue;
                }
                if (colNow >= width)
                {
                    colNow--;
                    continue;
                }
                rowNow = rowIndex - a / 2;
                for (int j = 0; j < a; j++)
                {
                    if (rowNow < 0)
                    {
                        rowNow++;
                        continue;
                    }
                    if (rowNow >= height)
                    {
                        rowNow--;
                        continue;
                    }
                    sum += arr[rowNow][colNow] * arr[rowNow][colNow];
                    rowNow++;
                    countPixInA++;
                }
                colNow++;
            }
            return sum / (double)countPixInA;
        }
    }

}



