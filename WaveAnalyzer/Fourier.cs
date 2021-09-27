using System;
using System.Collections.Generic;
using System.Text;

namespace WaveAnalyzer
{
    struct complex
    {
        public double real;
        public double imag;
    }

    static class Fourier
    {
        public static complex[] DFT(double[] s, int N)
        {
            complex[] A = new complex[N];

            for (int f = 0; f < N; ++f)
            {
                for (int t = 0; t < N; ++t)
                {
                    A[f].real += s[t] * Math.Cos(2 * Math.PI * t * f / N);
                    A[f].imag -= s[t] * Math.Sin(2 * Math.PI * t * f / N);
                }

                A[f].real /= N;
                A[f].imag /= N;
            }

            return A;
        }

        public static double[] inverseDFT(complex[] A, int N)
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
    }
}
