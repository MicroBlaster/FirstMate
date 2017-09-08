using System;
using System.Collections.Generic;
using System.Text;

namespace Terminal
{
    public class Display
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int[,] Data { get; private set; }

        public Display()
        {
            Height = 24;
            Width = 80;

            Data = new int[Height, Width];
        }

        public Display(int width, int height)
        {
            Height = width;
            Width = height;

            Data = new int[Height, Width];
        }

        public void Resize(int width, int height)
        {
            Height = width;
            Width = height;

            Data = new int[Height, Width];
        }

    }
}
