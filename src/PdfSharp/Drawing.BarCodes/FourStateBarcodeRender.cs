#region PDFsharp - A .NET library for processing PDF
//
// PDFsharp - A library for processing PDF
// 4-state Barcode
//
// Authors:
//   thagino
//
// Copyright (c) 2023 ACROSYSTEM Co.,Ltd. (Japan)
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

namespace PdfSharp.Drawing.BarCodes
{
    /// <summary>
    /// Internal base class for 4-state Barcode
    /// </summary>
    public abstract class FourStateBarcodeRender : BarCode
    {
        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="code"></param>
        /// <param name="direction"></param>
        public FourStateBarcodeRender(string code, CodeDirection direction)
            : base(code, XSize.Empty, direction)
         { }

        /// <summary>
        /// Track Height
        /// </summary>
        protected double _trackHeight = XUnit.FromMillimeter(1.2);

        /// <summary>
        /// Ascender Height
        /// </summary>
        protected double _ascenderHeight = XUnit.FromMillimeter(1.2);

        /// <summary>
        /// Bar width
        /// </summary>
        protected double _barWidth = XUnit.FromMillimeter(0.6);

        /// <summary>
        /// Bar space
        /// </summary>
        protected double _barSpace = XUnit.FromMillimeter(0.6);


        internal double TrackHeight
        {
            get { return _trackHeight; }
            set { _trackHeight = value; }
        }

        internal double AscenderHeight
        {
            get { return _ascenderHeight; }
            set { _ascenderHeight = value; }
        }

        internal double BarWidth
        {
            get { return _barWidth; }
            set { _barWidth = value; }
        }

        internal double BarSpace
        {
            get { return _barSpace; }
            set { _barSpace = value; }
        }

        internal override void InitRendering(BarCodeRenderInfo info)
        {
            if (Text == null)
                throw new InvalidOperationException(BcgSR.BarCodeNotSet);

            switch (Direction)
            {
                case CodeDirection.RightToLeft:
                    info.Gfx.RotateAtTransform(180, info.Position);
                    break;

                case CodeDirection.TopToBottom:
                    info.Gfx.RotateAtTransform(90, info.Position);
                    break;

                case CodeDirection.BottomToTop:
                    info.Gfx.RotateAtTransform(-90, info.Position);
                    break;
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
            info.CurrPos = position - CodeBase.CalcDistance(AnchorType.TopLeft, this.Anchor, this.Size);

            //Generates an encoding message from the code
            string messageStr = CreateMessage(Text);

            //Perform barcode level encoding
            string[] encodedStr = EncodeHighLevel(messageStr);

            foreach (string encstr in encodedStr)
            {
                foreach (char c in encstr)
                {
                    RenderBar(info, (int)char.GetNumericValue(c));
                    RenderGap(info);
                }
            }

            gfx.Restore(state);
        }


        /// <summary>
        /// Create a barcode message
        /// バーコードメッセージ作成
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected abstract string CreateMessage(string msg);

        /// <summary>
        /// Barcode level encoding
        /// バーコードレベルエンコード
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected abstract string[] EncodeHighLevel(String msg);

        /// <summary>
        /// Get the height of a bar code
        /// バーコード作画の高さを取得
        /// </summary>
        /// <param name="heightParam">Height parameter (0-3) 高さパラメータ(0～3)</param>
        /// <returns>height size</returns>
        /// <exception cref="ArgumentException"></exception>
        internal double getBarHeight(int heightParam)
        {
            switch (heightParam)
            {
                //0: track only
                case 0: return TrackHeight;
                //1: ascender
                case 1: return TrackHeight + AscenderHeight;
                //2: descender
                case 2: return TrackHeight + AscenderHeight;
                //3: full height
                case 3: return TrackHeight + (2 * AscenderHeight);
                default: throw new ArgumentException("Only height 0-3 allowed");
            }
        }

        /// <summary>
        /// Get the width of a bar code
        /// バーコードの幅を取得
        /// </summary>
        /// <returns>width size</returns>
        internal double GetBarWidth()
        {
            return BarWidth;
        }

        /// <summary>
        /// Render Barcode
        /// </summary>
        /// <param name="info">BarCodeRenderInfo</param>
        /// <param name="heightParam">height 4-state parameter</param>
        /// <exception cref="Exception"></exception>
        internal void RenderBar(BarCodeRenderInfo info, int heightParam)
        {
            double barWidth = GetBarWidth();
            double height = getBarHeight(heightParam);

            double middle = getBarHeight(0) / 2;
            double y1;
            switch (heightParam)
            {
                case 0:
                case 2:
                    y1 = middle - (getBarHeight(0) / 2);
                    break;
                case 1:
                case 3:
                    y1 = middle - (getBarHeight(3) / 2);
                    break;
                default:
                    throw new Exception("out of 4-state range");
            }
            double xPos = info.CurrPos.X;
            double yPos = info.CurrPos.Y;

            XRect rect = new XRect(xPos, yPos + y1, barWidth, height);
            info.Gfx.DrawRectangle(info.Brush, rect);
            info.CurrPos.X += barWidth;
        }

        /// <summary>
        /// Render gap
        /// </summary>
        /// <param name="info"></param>
        internal void RenderGap(BarCodeRenderInfo info)
        {
            info.CurrPos.X += BarSpace;
        }


    }
}
