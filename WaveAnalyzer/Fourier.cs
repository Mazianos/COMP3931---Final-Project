using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WaveAnalyzer
{
    /// <summary>
    /// A complex number
    /// </summary>
    public struct Complex
    {
        public double real;
        public double imag;

        public Complex(double real, double imag)
        {
            this.real = real;
            this.imag = imag;
        }

        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.real + b.real, a.imag + b.imag);
        }
    }

    /// <summary>
    /// A cosine wave
    /// </summary>
    public struct CosWave
    {
        public double amplitude;
        public double frequency;
        public double phaseShift;

        public CosWave(double amplitude, double frequency, double phaseShift)
        {
            this.amplitude = amplitude;
            this.frequency = frequency;
            this.phaseShift = phaseShift;
        }

        public double Calculate(int t, int N) => amplitude * Math.Cos((frequency * 2 * Math.PI * t / N) + phaseShift);
    }

    /// <summary>
    /// Data needed by the thread to complete DFT
    /// </summary>
    public struct ThreadData
    {
        public int first;
        public int second;
        public int N;
        public short[] s;

        public ThreadData(int first, int second, int N, short[] s)
        {
            this.first = first;
            this.second = second;
            this.N = N;
            this.s = s;
        }
    }

    /// <summary>
    /// Methods used to perform DFT and IDFT
    /// </summary>
    public static class Fourier
    {
        private const int DECIMAL_PLACES = 3;
        private const int Threads = 5;
        private static Complex[] A;

        /// <summary>
        /// DFT executed by a thread
        /// </summary>
        /// <param name="data">Portion of data passed to the thread to DFT</param>
        private static void DFTTask(object data)
        {
            ThreadData threadData = (ThreadData)data;
            int first = threadData.first;
            int second = threadData.second;
            int N = threadData.N;
            short[] s = threadData.s;

            for (int f = first; f < second; ++f)
            {
                for (int t = 0; t < N; t++)
                {
                    A[f].real += s[t] * Math.Cos(2 * Math.PI * t * f / N);
                    A[f].imag -= s[t] * Math.Sin(2 * Math.PI * t * f / N);
                }
            }
        }

        /// <summary>
        /// DFT an array of samples
        /// </summary>
        /// <param name="s">Array of samples to DFT</param>
        /// <param name="N">Number of samples we DFT</param>
        /// <param name="start">Unused</param>
        /// <returns></returns>
        public static Complex[] DFT(short[] s, int N, int start)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            A = new Complex[N];

            int firstIndex = 0;
            int secondIndex = 0;

            Task[] tasks = new Task[Threads];

            for (int i = 0; i < tasks.Length; ++i)
            {
                secondIndex += N / tasks.Length;

                Task t = Task.Factory.StartNew(DFTTask, new ThreadData(firstIndex, secondIndex, N, s));
                tasks[i] = t;

                firstIndex = secondIndex;
            }

            Task.WaitAll(tasks);
            sw.Stop();
            Trace.WriteLine("DFT took " + sw.ElapsedMilliseconds + " ms.");

            return A;
        }

        /// <summary>
        /// Undo that DFT
        /// </summary>
        /// <param name="A">Array of frequencies</param>
        /// <param name="N">Number of samples we IDFT</param>
        /// <returns></returns>
        public static double[] InverseDFT(Complex[] A, int N)
        {
            double[] s = new double[N];

            for (int t = 0; t < N; ++t)
            {
                for (int f = 0; f < N; ++f)
                {
                    s[t] += A[f].real * Math.Cos(2 * Math.PI * t * f / N);
                    s[t] -= A[f].imag * Math.Sin(2 * Math.PI * t * f / N);
                }
            }

            return s;
        }

        /// <summary>
        /// Prints array of complex nums to the console
        /// </summary>
        /// <param name="A">Array of complex nums</param>
        public static void PrintComplex(Complex[] A)
        {
            for (int i = 0; i < A.Length; ++i)
            {
                Trace.WriteLine("[" + i + "]\t" + Math.Round(A[i].real, DECIMAL_PLACES) + ",\t" + Math.Round(A[i].imag, DECIMAL_PLACES));
            }
        }

        /// <summary>
        /// Get the amplitudes of the frequencies
        /// </summary>
        /// <param name="A">Array of frequencies</param>
        /// <returns></returns>
        public static double[] GetAmplitudes(Complex[] A)
        {
            double[] amplitudes = new double[A.Length];
            for (int i = 0; i < amplitudes.Length; ++i)
            {
                amplitudes[i] = Pythagoras(A[i].real, A[i].imag);
            }

            return amplitudes;
        }

        /// <summary>
        /// Does a^2 + b^2 = c^2
        /// </summary>
        /// <param name="x">a</param>
        /// <param name="y">b</param>
        /// <returns></returns>
        public static double Pythagoras(double x, double y)
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        /// <summary>
        /// Divide the entire array by N
        /// </summary>
        /// <param name="A">Array of complex numbers</param>
        /// <param name="N">Length of the array</param>
        public static void DivideByN(Complex[] A, int N)
        {
            for (int i = 0; i < A.Length; ++i)
            {
                A[i].real /= N;
                A[i].imag /= N;
            }
        }
    }
}
