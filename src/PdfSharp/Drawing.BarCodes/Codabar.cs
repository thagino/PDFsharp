#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   stroup
//
// Copyright (c) 2010 stroup
//
// This source code file is copied from the below article:
//   https://forum.pdfsharp.net/viewtopic.php?f=2&t=1155
// (Modified a little by katz in order to make a success of building)
//
// For PDFsharp:
// http://www.pdfsharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Diagnostics;

namespace PdfSharp.Drawing.BarCodes
{
    /// <summary>
    /// Imlpementation of the codabar bar code.
    /// </summary>
    public class Codabar : ThickThinBarCode
    {
        /// <summary>
        /// Initializes a new instance of Codabar.
        /// </summary>
        public Codabar()
          : base("", XSize.Empty, CodeDirection.LeftToRight)
        {
        }

        /// <summary>
        /// Initializes a new instance of Codabar.
        /// </summary>
        public Codabar(string code)
          : base(code, XSize.Empty, CodeDirection.LeftToRight)
        {
        }

        /// <summary>
        /// Initializes a new instance of Codabar.
        /// </summary>
        public Codabar(string code, XSize size)
          : base(code, size, CodeDirection.LeftToRight)
        {
        }

        /// <summary>
        /// Initializes a new instance of Codabar.
        /// </summary>
        public Codabar(string code, XSize size, CodeDirection direction)
          : base(code, size, direction)
        {
        }

        /// <summary>
        /// Returns an array of size 7 that represents the thick (true) and thin (false) lines and spaces
        /// representing the specified digit.
        /// </summary>
        /// <param name="ch">The character to represent.</param>
        private static string ThickThinLines(char ch)
        {
            return Lines["0123456789-$:/.+ABCD".IndexOf(ch)];
        }
        static string[] Lines = new string[]
        {
      "0000011",    // '0'
      "0000110",    // '1'
      "0001001",    // '2'
      "1100000",    // '3'
      "0010010",    // '4'
      "1000010",    // '5'
      "0100001",    // '6'
      "0100100",    // '7'
      "0110000",    // '8'
      "1001000",    // '9'
      "0001100",    // '-'
      "0011000",    // '$'
      "1000101",    // ':'
      "1010001",    // '/'
      "1010100",    // '.'
      "0010101",    // '+'
      "0011010",    // 'A'
      "0101001",    // 'B'
      "0001011",    // 'C'
      "0001110",    // 'D'
       };


        /// <summary>
        /// Calculates the thick and thin line widths,
        /// taking into account the required rendering size.
        /// </summary>
        internal override void CalcThinBarWidth(BarCodeRenderInfo info)
        {

            string bars = "";
            foreach (char ch in StartChar + Text + EndChar)
            {
                bars += ThickThinLines(ch);
            }
            // 1's represent a thick gap or bar, 0 is thin gap or bar

            int thinbars = bars.Replace("1", "").Length;
            int thickbars = bars.Replace("0", "").Length + Text.Length + 1; //add thick gap for each character in the code and 1 extra for the start char

            double thinLineAmount = (this.WideNarrowRatio * thickbars) + thinbars;

            info.ThinBarWidth = this.Size.Width / thinLineAmount;
        }



        /// <summary>
        /// Checks the code to be convertible into an standard codabar.
        /// </summary>
        /// <param name="text">The code to be checked.</param>
        protected override void CheckCode(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            if (text.Length == 0)
                throw new ArgumentException(BcgSR.InvalidCodabar(text));

            foreach (char ch in text)
            {
                if ("0123456789-$:/.+ABCD".IndexOf(ch) < 0)
                    throw new ArgumentException(BcgSR.InvalidCodabar(text));
            }
        }

        /// <summary>
        /// Renders the bar code.
        /// </summary>
        protected internal override void Render(XGraphics gfx, XBrush brush, XFont font, XPoint position)
        {
            XGraphicsState state = gfx.Save();

            BarCodeRenderInfo info = new BarCodeRenderInfo(gfx, brush, font, position);
            InitRendering(info);
            info.CurrPosInString = 0;
            //info.CurrPos = info.Center - this.size / 2;
            info.CurrPos = position - CodeBase.CalcDistance(AnchorType.TopLeft, this.Anchor, this.Size);

            RenderStart(info);

            while (info.CurrPosInString < this.Text.Length)
            {
                RenderNextChar(info);
                RenderGap(info, true);
            }
            RenderStop(info);
            if (TextLocation != TextLocation.None)
                RenderText(info);

            gfx.Restore(state);
        }

        private void RenderNextChar(BarCodeRenderInfo info)
        {
            RenderChar(info, this.Text[info.CurrPosInString]);
            ++info.CurrPosInString;
        }

        private void RenderChar(BarCodeRenderInfo info, char ch)
        {
            string thickThinLines = ThickThinLines(ch);
            int idx = 0;
            while (idx < 7)
            {
                if ((idx & 1) == 0)
                {
                    RenderBar(info, thickThinLines.Substring(idx, 1).Contains("1"));
                }
                else
                {
                    RenderGap(info, thickThinLines.Substring(idx, 1).Contains("1"));
                }

                idx += 1;
            }
            //RenderGap(info, false);
        }

        private void RenderStart(BarCodeRenderInfo info)
        {
            RenderChar(info, StartChar);
            RenderGap(info, true);
        }

        private void RenderStop(BarCodeRenderInfo info)
        {
            RenderChar(info, EndChar);
        }
    }
}
