using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace SCOI_3
{
    public class ImgBytes
    {
        protected byte[] bytes1;

        protected int _stride;
        protected int _height;
        protected int _width;
        private PixelFormat _pixelFormat;
        private int _bitsPerPixel;

        public bool HasMinValue = false;

        public PixelFormat PixelFormat { get => _pixelFormat; }
        public int BitsPerPixel { get => _bitsPerPixel; }

        public BitmapSource BitmapSourseOrig { get => GetBitmapSource(bytes1); }
        public Bitmap BitmapOrig { get => GetBitmap(bytes1); }


        /// <summary>
        /// Инициализация. 
        /// bool - Применяет Bitmap.Dispose в конце функции или нет
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="DisposeThisBitmap"></param>
        public ImgBytes(Bitmap Source, bool DisposeThisBitmap = true)
        {
            var bmpData = Source.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            _stride = bmpData.Stride;
            _height = Source.Height;
            _width = Source.Width;
            bytes1 = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes1, 0, Size);
            Source.UnlockBits(bmpData);
            _pixelFormat = Source.ToBitmapSource().Format;
            _bitsPerPixel = PixelFormat.BitsPerPixel;
            if (DisposeThisBitmap)
                Source.Dispose();
        }

        public ImgBytes(BitmapSource Source)
        {
            if (Source.Format.BitsPerPixel < 24)
            {
                ImageConvert.To24rgb(ref Source);
            }
            bytes1 = Source.ToByte(out _stride, out _height, out _width, out _pixelFormat);
            _bitsPerPixel = _pixelFormat.BitsPerPixel;
        }

        private BitmapSource GetBitmapSource(byte[] b)
        {
            var Source = b.ToBitmapSource(_stride, _width, _height, _pixelFormat);
            return Source;
        }

        private Bitmap GetBitmap(byte[] b, bool min = false)
        {
            var Source = b.ToBitmapSource(_stride, _width, _height, _pixelFormat);
            return Source.ToBitmap();
        }
    }

    public class ImgBinary : ImgBytes
    {
        byte[] bytesOriginal;

        ulong[] _integralMatrix;
        ulong[] _integralMatrixSqr;

        byte _minPixel = 255;

        public bool RememberIntegralMatrix = true;
        public bool RememberIntegralSqrMatrix = false;

        public void DeleteMatrix()
        {
            _integralMatrix = null;
        }

        public void DeleteMatrixSqr()
        {
            _integralMatrixSqr = null;
        }

        public void CheckAndDeleteMatrix()
        {
            if (RememberIntegralMatrix == false)
                DeleteMatrix();
            if (RememberIntegralSqrMatrix == false)
                DeleteMatrixSqr();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        public BinaryOption BinaryOption { get; set; }

        public ImgBinary(Bitmap source, bool DisposeThisBitmap = true) : base(source, DisposeThisBitmap)
        {
            bytesOriginal = bytes1.Clone() as byte[];
            BinaryCPP.ToGray(bytes1, bytes1.Length, BitsPerPixel, ref _minPixel);
        }

        public ImgBinary(BitmapSource source) : base(source)
        {
            bytesOriginal = bytes1.Clone() as byte[];
            BinaryCPP.ToGray(bytes1, bytes1.Length, BitsPerPixel, ref _minPixel);
        }

        public BitmapSource BinaryGlobal()
        {
            int ink = BitsPerPixel / 8;
            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = bytes1[i + 3];
                }
            }

            BinaryCPP.Gavrilov(bytes1, bClone, bClone.Length, _width, _height, BitsPerPixel);

            BinaryOption = BinaryOption.GLobal;
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public BitmapSource BinaryOtsu2()
        {
            int ink = BitsPerPixel / 8;
            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            double[] gist = new double[256];
            int size = _width * _height;

            for (int i = 0; i < bClone.Length; i += ink)
            {
                gist[bytes1[i]] += 1;
            }

            for (int i = 0; i < 256; i++)
            {
                gist[i] /= size;
            }

            double o_max = -1;
            double t = 0;
            double uT = SumMult(gist, 256);

            for (int i = 0; i < 256; i++)
            {
                double w1 = Sum(gist, i);
                double w2 = 1 - w1;
                double u1 = SumMult(gist, i) / w1;
                double u2 = (uT - u1 * w1) / w2;
                double o = w1 * w2 * Math.Pow((u1 - u2), 2);
                if (o > o_max)
                {
                    o_max = o;
                    t = i;
                }
            }

            for (int i = 0; i < bClone.Length; i += ink)
            {
                if (bytes1[i] <= t)
                {
                    bClone[i] = 0;
                    bClone[i + 1] = 0;
                    bClone[i + 2] = 0;
                }
                else
                {
                    bClone[i] = 255;
                    bClone[i + 1] = 255;
                    bClone[i + 2] = 255;
                }
            }

            BinaryOption = BinaryOption.Otsu;
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public static double Sum(double[] arr, int finallyIndex = 255)
        {
            double res = 0;
            for (int i = 0; i < finallyIndex; i++)
            {
                res += arr[i];
            }
            return res;
        }

        public static double SumMult(double[] arr, int finallyIndex = 255)
        {
            double res = 0;
            for (int i = 0; i < finallyIndex; i++)
            {
                res += arr[i] * i;
            }
            return res;
        }

        public BitmapSource BinaryOtsu()
        {
            int ink = BitsPerPixel / 8;
            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            BinaryCPP.Otsu(bytes1, bClone, bClone.Length, _width, _height, BitsPerPixel);

            BinaryOption = BinaryOption.Otsu;
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public BitmapSource BinaryNiblek(int a = 3, double k = -0.2)
        {
            int ink = BitsPerPixel / 8;

            if (_integralMatrix == null && _integralMatrixSqr == null)
            {
                _integralMatrix = new ulong[_width * _height];
                _integralMatrixSqr = new ulong[_width * _height];
                BinaryCPP.CalcIntegralMatrix2(bytes1, _width, _height, BitsPerPixel, _integralMatrix, _integralMatrixSqr);
            }
            else
            {
                if (_integralMatrix == null)
                {
                    _integralMatrix = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrix);
                }
                if (_integralMatrixSqr == null)
                {
                    _integralMatrixSqr = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralSqrMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrixSqr);
                }
            }

            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            BinaryCPP.Niblek(bytes1, bClone, bytes1.Length, _integralMatrix, _integralMatrixSqr, _width, _height, BitsPerPixel, a, k);

            BinaryOption = BinaryOption.Niblek;
            if (RememberIntegralMatrix == false)
                DeleteMatrix();
            if (RememberIntegralSqrMatrix == false)
                DeleteMatrixSqr();
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public BitmapSource BinarySauvola(int a = 3, double k = 0.2)
        {
            int ink = BitsPerPixel / 8;

            if (_integralMatrix == null && _integralMatrixSqr == null)
            {
                _integralMatrix = new ulong[_width * _height];
                _integralMatrixSqr = new ulong[_width * _height];
                BinaryCPP.CalcIntegralMatrix2(bytes1, _width, _height, BitsPerPixel, _integralMatrix, _integralMatrixSqr);
            }
            else
            {
                if (_integralMatrix == null)
                {
                    _integralMatrix = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrix);
                }
                if (_integralMatrixSqr == null)
                {
                    _integralMatrixSqr = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralSqrMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrixSqr);
                }
            }

            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            BinaryCPP.Sauvola(bytes1, bClone, bClone.Length, _integralMatrix, _integralMatrixSqr, _width, _height, BitsPerPixel, a, k);

            BinaryOption = BinaryOption.Sauvola;
            if (RememberIntegralMatrix == false)
                DeleteMatrix();
            if (RememberIntegralSqrMatrix == false)
                DeleteMatrixSqr();
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public BitmapSource BinaryKristianWolf(int a = 3, double k = 0.5)
        {
            int ink = BitsPerPixel / 8;

            if (_integralMatrix == null && _integralMatrixSqr == null)
            {
                _integralMatrix = new ulong[_width * _height];
                _integralMatrixSqr = new ulong[_width * _height];
                BinaryCPP.CalcIntegralMatrix2(bytes1, _width, _height, BitsPerPixel, _integralMatrix, _integralMatrixSqr);
            }
            else
            {
                if (_integralMatrix == null)
                {
                    _integralMatrix = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrix);
                }
                if (_integralMatrixSqr == null)
                {
                    _integralMatrixSqr = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralSqrMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrixSqr);
                }
            }

            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            BinaryCPP.KristianWolf(bytes1, bClone, bClone.Length, _integralMatrix, _integralMatrixSqr, _width, _height, BitsPerPixel, a, k, _minPixel);

            BinaryOption = BinaryOption.KristianWolf;
            if (RememberIntegralMatrix == false)
                DeleteMatrix();
            if (RememberIntegralSqrMatrix == false)
                DeleteMatrixSqr();
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public BitmapSource BinaryBradley(int a = 3, double k = 0.15)
        {
            int ink = BitsPerPixel / 8;

            if (_integralMatrix == null)
            {
                _integralMatrix = new ulong[_width * _height];
                BinaryCPP.CalcIntegralMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrix);
            }

            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            BinaryCPP.Bradley(bytes1, bClone, bClone.Length, _integralMatrix, _width, _height, BitsPerPixel, a, k);

            BinaryOption = BinaryOption.Bradley;
            if (RememberIntegralMatrix == false)
                DeleteMatrix();
            if (RememberIntegralSqrMatrix == false)
                DeleteMatrixSqr();
            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        public BitmapSource Binary(BinaryOption option)
        {
            switch (option)
            {
                case BinaryOption.GLobal:
                    return BinaryGlobal();
                case BinaryOption.Otsu:
                    return BinaryOtsu();
                case BinaryOption.Niblek:
                    return BinaryNiblek();
                case BinaryOption.Sauvola:
                    return BinarySauvola();
                case BinaryOption.KristianWolf:
                    return BinaryKristianWolf();
                case BinaryOption.Bradley:
                    return BinaryBradley();
                default:
                    return BinaryOtsu();
            }
        }

        public BitmapSource Binary(BinaryOption option, int rect, double param)
        {
            switch (option)
            {
                case BinaryOption.GLobal:
                    return BinaryGlobal();
                case BinaryOption.Otsu:
                    return BinaryOtsu2();
                case BinaryOption.Niblek:
                    return BinaryNiblek(rect, param);
                case BinaryOption.Sauvola:
                    return BinarySauvola(rect, param);
                case BinaryOption.KristianWolf:
                    return BinaryKristianWolf(rect, param);
                case BinaryOption.Bradley:
                    return BinaryBradley(rect, param);
                default:
                    return BinaryOtsu();
            }
        }

        /// <summary>
        /// Удаление тонких линий и пятен
        /// Выравнивание текста на картинке по горизонтали
        /// </summary>
        /// <returns></returns>
        public BitmapSource TreshHold(int rectan = 60, double a = 0.2)
        {
            BinarySauvola(60, 0.2);
            int ink = BitsPerPixel / 8;

            if (_integralMatrix == null && _integralMatrixSqr == null)
            {
                _integralMatrix = new ulong[_width * _height];
                _integralMatrixSqr = new ulong[_width * _height];
                BinaryCPP.CalcIntegralMatrix2(bytes1, _width, _height, BitsPerPixel, _integralMatrix, _integralMatrixSqr);
            }
            else
            {
                if (_integralMatrix == null)
                {
                    _integralMatrix = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrix);
                }
                if (_integralMatrixSqr == null)
                {
                    _integralMatrixSqr = new ulong[_width * _height];
                    BinaryCPP.CalcIntegralSqrMatrix(bytes1, _width, _height, BitsPerPixel, _integralMatrixSqr);
                }
            }

            byte[] bClone = new byte[bytes1.Length];

            //Задаем альфа-канал, если он есть
            if (BitsPerPixel == 32)
            {
                for (int i = 0; i < bytes1.Length; i += ink)
                {
                    bClone[i + 3] = 255;
                }
            }

            BinaryCPP.Bradley(bytes1, bClone, bClone.Length, _integralMatrix, _width, _height, BitsPerPixel, 198, 0.15);

            BinaryOption = BinaryOption.Sauvola;
            if (RememberIntegralMatrix == false)
                DeleteMatrix();
            if (RememberIntegralSqrMatrix == false)
                DeleteMatrixSqr();

            int MinPixel = 0;
            int rect = 7;
            for (int i = 0; i < bClone.Length; i += ink)
            {
                int rowIndex = (i / (BitsPerPixel / 8)) / _width;
                int colIndex = (i / (BitsPerPixel / 8)) % _width;

                Point LeftUpper = new Point(colIndex - rect / 2, rowIndex - rect / 2);
                Point RightBot = new Point(colIndex + rect / 2, rowIndex + rect / 2);

                //Производим действие с матрицей
                int res = 0;
                int rownow = LeftUpper.Y;
                int colnow = LeftUpper.X;
                for (int imatrix = 0; imatrix < rect; imatrix++)
                {
                    colnow = LeftUpper.X;
                    for (int jmatrix = 0; jmatrix < rect; jmatrix++)
                    {
                        int rownowtmp = rownow < 0 ? -rownow - 1 : rownow;
                        int colnowtmp = colnow < 0 ? -colnow - 1 : colnow;
                        if (rownowtmp >= _height)
                        {
                            rownowtmp -= 1 + (rownowtmp - _height);
                        }
                        if (colnowtmp >= _width)
                        {
                            colnowtmp -= 1 + (colnowtmp - _width);
                        }
                        res += bClone[rownowtmp * _width * ink + colnowtmp * ink] / 254;
                        colnow++;
                    }
                    rownow++;
                }
                if (res >= 41)
                {
                    bClone[i] = 255;
                    bClone[i + 1] = 255;
                    bClone[i + 2] = 255;
                }
                int rownowtmp2 = rownow < 0 ? -rownow - 1 : rownow;
                int colnowtmp2 = colnow < 0 ? -colnow - 1 : colnow;
                if (rownowtmp2 >= _height)
                {
                    rownowtmp2 -= 1 + (rownowtmp2 - _height);
                }
                if (colnowtmp2 >= _width)
                {
                    colnowtmp2 -= 1 + (colnowtmp2 - _width);
                }
                if (bClone[rownowtmp2 * _width * ink + colnowtmp2 * ink] / 254 == 0 && rownowtmp2 > MinPixel)
                {
                    MinPixel = rownowtmp2;
                }
            }

            for(int i = 0; i < bClone.Length; i += ink)
            {
                if(bClone[i] == 0)
                {
                    bClone[i] = bytesOriginal[i];
                    bClone[i + 1] = bytesOriginal[i + 1];
                    bClone[i + 2] = bytesOriginal[i + 2];
                }
            }

            return bClone.ToBitmapSource(_stride, _width, _height, PixelFormat);
        }

        private int SumIntegralMatrix(int[][] matrix, int rect, int indexByte, out int C)
        {
            int rowIndex = (indexByte / (BitsPerPixel / 8)) / _width;
            int colIndex = (indexByte / (BitsPerPixel / 8)) % _width;

            Point LeftUpper = new Point(colIndex - rect / 2, rowIndex - rect / 2);
            Point RightBot = new Point(colIndex + rect / 2, rowIndex + rect / 2);

            int a = -1;
            int b = a;
            int c = a;
            int d = a;

            if (LeftUpper.X <= 0)
            {
                LeftUpper.X = 0;
                a = b = 0;
            }

            if (LeftUpper.Y <= 0)
            {
                LeftUpper.Y = 0;
                a = c = 0;
            }

            if (RightBot.X >= _width)
            {
                RightBot.X = _width - 1;
            }

            if (RightBot.Y >= _height)
            {
                RightBot.Y = _height - 1;
            }

            if (d == -1)
                d = matrix[RightBot.Y][RightBot.X];
            if (a == -1)
                a = matrix[LeftUpper.Y - 1][LeftUpper.X - 1];
            if (c == -1)
                c = matrix[LeftUpper.Y - 1][RightBot.X];
            if (b == -1)
                b = matrix[RightBot.Y][LeftUpper.X - 1];

            C = (RightBot.X - LeftUpper.X + 1) * (RightBot.Y - LeftUpper.Y + 1);

            return (a + d - b - c);
        }

        private double MiddleIntegralMatrix(int[][] matrix, int rect, int indexByte)
        {
            int rowIndex = (indexByte / (BitsPerPixel / 8)) / _width;
            int colIndex = (indexByte / (BitsPerPixel / 8)) % _width;

            Point LeftUpper = new Point(colIndex - rect / 2, rowIndex - rect / 2);
            Point RightBot = new Point(colIndex + rect / 2, rowIndex + rect / 2);

            int a = -1;
            int b = a;
            int c = a;
            int d = a;

            if (LeftUpper.X <= 0)
            {
                LeftUpper.X = 0;
                a = b = 0;
            }

            if (LeftUpper.Y <= 0)
            {
                LeftUpper.Y = 0;
                a = c = 0;
            }

            if (RightBot.X >= _width)
            {
                RightBot.X = _width - 1;
            }

            if (RightBot.Y >= _height)
            {
                RightBot.Y = _height - 1;
            }

            if (d == -1)
                d = matrix[RightBot.Y][RightBot.X];
            if (a == -1)
                a = matrix[LeftUpper.Y - 1][LeftUpper.X - 1];
            if (c == -1)
                c = matrix[LeftUpper.Y - 1][RightBot.X];
            if (b == -1)
                b = matrix[RightBot.Y][LeftUpper.X - 1];

            int CountSumPix = (RightBot.X - LeftUpper.X + 1) * (RightBot.Y - LeftUpper.Y + 1);

            return (a + d - b - c) / (double)CountSumPix;
        }

        private double MiddleSqrIntegralMatrix(int[][] matrix, int rect, int indexByte)
        {
            int rowIndex = (indexByte / (BitsPerPixel / 8)) / _width;
            int colIndex = (indexByte / (BitsPerPixel / 8)) % _width;

            Point LeftUpper = new Point(colIndex - rect / 2, rowIndex - rect / 2);
            Point RightBot = new Point(colIndex + rect / 2, rowIndex + rect / 2);

            int a = -1;
            int b = a;
            int c = a;
            int d = a;

            if (LeftUpper.X <= 0)
            {
                LeftUpper.X = 0;
                a = b = 0;
            }

            if (LeftUpper.Y <= 0)
            {
                LeftUpper.Y = 0;
                a = c = 0;
            }

            if (RightBot.X >= _width)
            {
                RightBot.X = _width - 1;
            }

            if (RightBot.Y >= _height)
            {
                RightBot.Y = _height - 1;
            }

            if (d == -1)
                d = matrix[RightBot.Y][RightBot.X];
            if (a == -1)
                a = matrix[LeftUpper.Y - 1][LeftUpper.X - 1];
            if (c == -1)
                c = matrix[LeftUpper.Y - 1][RightBot.X];
            if (b == -1)
                b = matrix[RightBot.Y][LeftUpper.X - 1];

            int CountSumPix = (RightBot.X - LeftUpper.X + 1) * (RightBot.Y - LeftUpper.Y + 1);

            return (a + d - b - c) / (double)CountSumPix;
        }

        private double DispersionIntegralMatrix(int[][] matrix, int[][] matrixsqr, int rect, int indexByte, out double M)
        {
            int rowIndex = (indexByte / (BitsPerPixel / 8)) / _width;
            int colIndex = (indexByte / (BitsPerPixel / 8)) % _width;

            Point LeftUpper = new Point(colIndex - rect / 2, rowIndex - rect / 2);
            Point RightBot = new Point(colIndex + rect / 2, rowIndex + rect / 2);

            int a = -1;
            int b = a;
            int c = a;
            int d = a;


            int a1 = -1;
            int b1 = a;
            int c1 = a;
            int d1 = a;

            if (LeftUpper.X <= 0)
            {
                LeftUpper.X = 0;
                a = b = a1 = b1 = 0;
            }

            if (LeftUpper.Y <= 0)
            {
                LeftUpper.Y = 0;
                a = c = a1 = c1 = 0;
            }

            if (RightBot.X >= _width)
            {
                RightBot.X = _width - 1;
            }

            if (RightBot.Y >= _height)
            {
                RightBot.Y = _height - 1;
            }

            if (d == -1)
            {
                d = matrix[RightBot.Y][RightBot.X];
                d1 = matrixsqr[RightBot.Y][RightBot.X];
            }
            if (a == -1)
            {
                a = matrix[LeftUpper.Y - 1][LeftUpper.X - 1];
                a1 = matrixsqr[LeftUpper.Y - 1][LeftUpper.X - 1];
            }
            if (c == -1)
            {
                c = matrix[LeftUpper.Y - 1][RightBot.X];
                c1 = matrixsqr[LeftUpper.Y - 1][RightBot.X];
            }
            if (b == -1)
            {
                b = matrix[RightBot.Y][LeftUpper.X - 1];
                b1 = matrixsqr[RightBot.Y][LeftUpper.X - 1];
            }

            int CountSumPix = (RightBot.X - LeftUpper.X + 1) * (RightBot.Y - LeftUpper.Y + 1);
            M = (a + d - b - c) / (double)CountSumPix;

            return ((a1 + d1 - b1 - c1) / (double)CountSumPix) - M * M;
        }

        private int[][] calcIntegralMatrix(byte[][] bytes)
        {
            var res = new int[bytes.Length][];

            for (int i = 0; i < bytes.Length; i++)
            {
                res[i] = new int[bytes[0].Length];
            }

            res[0][0] = bytes[0][0];

            //первый столбец
            for (int i = 1; i < bytes.Length; i++)
            {
                res[i][0] = bytes[i][0] + res[i - 1][0];
            }

            //первая строка
            for (int i = 1; i < bytes[0].Length; i++)
            {
                res[0][i] = bytes[0][i] + res[0][i - 1];
            }

            for (int i = 1; i < bytes.Length; i++)
            {
                for (int j = 1; j < bytes[0].Length; j++)
                {
                    res[i][j] = bytes[i][j] + res[i - 1][j] + res[i][j - 1] - res[i - 1][j - 1];
                }
            }
            return res;
        }

        private int[][] calcIntegralMatrixSqr(byte[][] bytes)
        {
            var sqrMatrix = new int[bytes.Length][];

            for (int i = 0; i < bytes.Length; i++)
            {
                sqrMatrix[i] = new int[bytes[0].Length];
            }

            sqrMatrix[0][0] = bytes[0][0] * bytes[0][0];

            //первый столбец
            for (int i = 1; i < bytes.Length; i++)
            {
                sqrMatrix[i][0] = bytes[i][0] * bytes[i][0] + sqrMatrix[i - 1][0];
            }

            //первая строка
            for (int i = 1; i < bytes[0].Length; i++)
            {
                sqrMatrix[0][i] = bytes[0][i] * bytes[0][i] + sqrMatrix[0][i - 1];
            }

            for (int i = 1; i < bytes.Length; i++)
            {
                for (int j = 1; j < bytes[0].Length; j++)
                {
                    sqrMatrix[i][j] = bytes[i][j] * bytes[i][j] + sqrMatrix[i - 1][j] + sqrMatrix[i][j - 1] - sqrMatrix[i - 1][j - 1];
                }
            }
            return sqrMatrix;
        }

        private int[][] calcIntegralMatrix(byte[][] bytes, ref int[][] sqrMatrix)
        {
            var res = new int[bytes.Length][];
            sqrMatrix = new int[bytes.Length][];

            for (int i = 0; i < bytes.Length; i++)
            {
                res[i] = new int[bytes[0].Length];
                sqrMatrix[i] = new int[bytes[0].Length];
            }

            res[0][0] = bytes[0][0];
            sqrMatrix[0][0] = bytes[0][0] * bytes[0][0];

            //первый столбец
            for (int i = 1; i < bytes.Length; i++)
            {
                res[i][0] = bytes[i][0] + res[i - 1][0];
                sqrMatrix[i][0] = bytes[i][0] * bytes[i][0] + sqrMatrix[i - 1][0];
            }

            //первая строка
            for (int i = 1; i < bytes[0].Length; i++)
            {
                res[0][i] = bytes[0][i] + res[0][i - 1];
                sqrMatrix[0][i] = bytes[0][i] * bytes[0][i] + sqrMatrix[0][i - 1];
            }

            for (int i = 1; i < bytes.Length; i++)
            {
                for (int j = 1; j < bytes[0].Length; j++)
                {
                    res[i][j] = bytes[i][j] + res[i - 1][j] + res[i][j - 1] - res[i - 1][j - 1];
                    sqrMatrix[i][j] = bytes[i][j] * bytes[i][j] + sqrMatrix[i - 1][j] + sqrMatrix[i][j - 1] - sqrMatrix[i - 1][j - 1];
                }
            }
            return res;
        }
    }

    public enum BinaryOption
    {
        GLobal,
        Otsu,
        Niblek,
        Sauvola,
        KristianWolf,
        Bradley
    }

    /// <summary>
    /// Test
    /// </summary>
    public class Img2
    {
        protected BitmapSource _img;

        public BitmapSource BitmapSourse { get => _img; }
        public Bitmap Bitmap { get => GetBitmap(); }
        public int Stride { get => (int)(_img.PixelWidth * (_img.Format.BitsPerPixel / 8.0)); }

        public Img2(string path)
        {
            _img = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            if (_img.Format.BitsPerPixel < 24)
            {
                ImageConvert.To24rgb(ref _img);
            }
        }

        private Bitmap GetBitmap()
        {
            return _img.ToBitmap();
        }
    }

    public class Img2Binary : Img2
    {
        int[][] _integralMatrix;

        public Img2Binary(string path) : base(path) { }

        public BinaryOption BinaryOption { get; set; }

        public BitmapSource BinaryGlobal()
        {
            long sum = 0;
            byte[] bytes1 = _img.ToByte();
            switch (_img.Format.BitsPerPixel)
            {
                case 32:
                    for (int i = 0; i < bytes1.Length; i += 4)
                    {
                        sum += bytes1[i];
                        sum += bytes1[i + 1];
                        sum += bytes1[i + 2];
                    }
                    break;
                case 24:
                    for (int i = 0; i < bytes1.Length; i += 3)
                    {
                        sum += bytes1[i];
                        sum += bytes1[i + 1];
                        sum += bytes1[i + 2];
                    }
                    break;
                default:
                    for (int i = 0; i < bytes1.Length; i += 3)
                    {
                        sum += bytes1[i];
                        sum += bytes1[i + 1];
                        sum += bytes1[i + 2];
                    }
                    break;
            }
            byte t = (byte)(sum / (double)(_img.PixelWidth * _img.PixelHeight));
            byte[] bClone = new byte[bytes1.Length];
            switch (_img.Format.BitsPerPixel)
            {
                case 32:
                    for (int i = 0; i < bytes1.Length; i += 4)
                    {
                        if (bytes1[i] <= t)
                        {
                            bClone[i] = 0;
                            bClone[i + 1] = 0;
                            bClone[i + 2] = 0;
                            bClone[i + 3] = bytes1[i + 3];
                        }
                        else
                        {
                            bClone[i] = 255;
                            bClone[i + 1] = 255;
                            bClone[i + 2] = 255;
                            bClone[i + 3] = bytes1[i + 3];
                        }
                    }
                    break;
                case 24:
                    for (int i = 0; i < bytes1.Length; i += 3)
                    {
                        if (bytes1[i] <= t)
                        {
                            bClone[i] = 0;
                            bClone[i + 1] = 0;
                            bClone[i + 2] = 0;
                        }
                        else
                        {
                            bClone[i] = 255;
                            bClone[i + 1] = 255;
                            bClone[i + 2] = 255;
                        }
                    }
                    break;
                default:
                    for (int i = 0; i < bytes1.Length; i += 3)
                    {
                        if (bytes1[i] <= t)
                        {
                            bClone[i] = 0;
                            bClone[i + 1] = 0;
                            bClone[i + 2] = 0;
                        }
                        else
                        {
                            bClone[i] = 255;
                            bClone[i + 1] = 255;
                            bClone[i + 2] = 255;
                        }
                    }
                    break;
            }
            BinaryOption = BinaryOption.GLobal;
            return bClone.ToBitmapSource(Stride, _img.PixelWidth, _img.PixelHeight, _img.Format);
        }

        public BitmapSource BinaryNiblek(int a = 7, double k = -0.1)
        {
            byte[] bytes1 = _img.ToByte();
            var p = bytes1.To2xArray(_img.PixelHeight, _img.PixelWidth, _img.Format.BitsPerPixel);
            if (_integralMatrix == null)
            {
                _integralMatrix = calcIntegralMatrix(p);
            }
            double M;
            double o;
            byte t;
            double D = 0;
            byte[] bClone = new byte[bytes1.Length];
            for (int i = 0; i < bytes1.Length; i += 4)
            {
                M = _helper.SumMatrix(p, i, _img.PixelHeight, _img.PixelWidth, a, _img.Format.BitsPerPixel);
                D = _helper.SumMatrixSqr(p, i, _img.PixelHeight, _img.PixelWidth, a, _img.Format.BitsPerPixel) - M * M;
                o = Math.Sqrt(D);
                t = (byte)(M + k * o);

                if (bytes1[i] <= t)
                {
                    bClone[i] = 0;
                    bClone[i + 1] = 0;
                    bClone[i + 2] = 0;
                    bClone[i + 3] = bytes1[i + 3];
                }
                else
                {
                    bClone[i] = 255;
                    bClone[i + 1] = 255;
                    bClone[i + 2] = 255;
                    bClone[i + 3] = bytes1[i + 3];
                }
            }
            BinaryOption = BinaryOption.Niblek;
            return bClone.ToBitmapSource(Stride, _img.PixelWidth, _img.PixelHeight, _img.Format);
        }

        private int[][] calcIntegralMatrix(byte[][] bytes)
        {
            var res = new int[bytes.Length][];

            for (int i = 0; i < bytes.Length; i++)
            {
                res[i] = new int[bytes[0].Length];
            }

            res[0][0] = bytes[0][0];

            //первый столбец
            for (int i = 1; i < bytes.Length; i++)
            {
                res[i][0] = bytes[i][0] + res[i - 1][0];
            }

            //первая строка
            for (int i = 1; i < bytes[0].Length; i++)
            {
                res[0][i] = bytes[0][i] + res[0][i - 1];
            }

            for (int i = 1; i < bytes.Length; i++)
            {
                res[i] = new int[bytes[0].Length];
                for (int j = 1; j < bytes[0].Length; j++)
                {
                    res[i][j] = bytes[i][j] + res[i - 1][j] + res[i][j - 1] - res[i - 1][j - 1];
                }
            }
            return res;
        }
    }
}
