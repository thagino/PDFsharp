#region PDFsharp - A .NET library for processing PDF
//
// PDFsharp - A library for processing PDF
// Japan Post 4-state Barcode (Customer Barcode)
//
// Authors:
//   thagino
//
// Copyright (c) 2023 ACROSYSTEM Co.,Ltd. (Japan)
//
// This implementation logic was adapted from the following description.
// 
// ZipConvertCustomerBarCode
// https://github.com/cssho/ZipConvertCustomerBarCode
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfSharp.Drawing.BarCodes
{
    public class JapanPostCB : FourStateBarcodeRender
    {
        /// <summary>
        /// 4-state Line Value
        /// 4-state ライン値
        /// </summary>
        private static readonly Dictionary<char, string> LINES = new Dictionary<char, string>()
        {
            //0 = Track only, 1 = Ascender, 2 = Descender, 3 = 1 + 2 = Full height
            {'[', "32"},
            { ']', "23" },
            { '(', "32" },
            { ')', "23" },
            { '0', "300" },
            { '1', "330" },
            { '2', "321" },
            { '3', "231" },
            { '4', "312" },
            { '5', "303" },
            { '6', "213" },
            { '7', "132" },
            { '8', "123" },
            { '9', "033" },
            { '-', "030" },
            { '₁', "210" },  //CC1
            { '₂', "201" },  //CC2
            { '₃', "120" },  //CC3
            { '₄', "021" },  //CC4
            { '₅', "102" },  //CC5
            { '₆', "012" },  //CC6
            { '₇', "003" },  //CC7
            { '₈', "333" }   //CC8
        };

        /// <summary>
        /// Full-width half-width substitution within a specific range.
        /// 全角半角置換（範囲限定）
        /// </summary>
        private static readonly Dictionary<char, char> FULLWIDTH_HALFWIDTH = new Dictionary<char, char>() {
            {'１','1'},{'２','2'},{'３','3'},{'４','4'},{'５','5'},
            {'６','6'},{'７','7'},{'８','8'},{'９','9'},{'０','0'},
            {'Ａ','A'},{'Ｂ','B'},{'Ｃ','C'},{'Ｄ','D'},{'Ｅ','E'},
            {'Ｆ','F'},{'Ｇ','G'},{'Ｈ','H'},{'Ｉ','I'},{'Ｊ','J'},
            {'Ｋ','K'},{'Ｌ','L'},{'Ｍ','M'},{'Ｎ','N'},{'Ｏ','O'},
            {'Ｐ','P'},{'Ｑ','Q'},{'Ｒ','R'},{'Ｓ','S'},{'Ｔ','T'},
            {'Ｕ','U'},{'Ｖ','V'},{'Ｗ','W'},{'Ｘ','X'},{'Ｙ','Y'},
            {'Ｚ','Z'},
            {'ａ','a'},{'ｂ','b'},{'ｃ','c'},{'ｄ','d'},{'ｅ','e'},
            {'ｆ','f'},{'ｇ','g'},{'ｈ','h'},{'ｉ','i'},{'ｊ','j'},
            {'ｋ','k'},{'ｌ','l'},{'ｍ','m'},{'ｎ','n'},{'ｏ','o'},
            {'ｐ','p'},{'ｑ','q'},{'ｒ','r'},{'ｓ','s'},{'ｔ','t'},
            {'ｕ','u'},{'ｖ','v'},{'ｗ','w'},{'ｘ','x'},{'ｙ','y'},
            {'ｚ','z'},
            {'　',' '},{'‐','-'}
        };

        /// <summary>
        /// Alphabetic Range Definition.
        /// アルファベット定義
        /// </summary>
        private static readonly string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Characters to assign when calculating check digits.
        /// チェックディジット計算時の割り当て文字
        /// </summary>
        private static readonly string BARCODECHR = "0123456789-₁₂₃₄₅₆₇₈";

        /// <summary>
        /// Numbers that correspond to assigned letters when calculating check digits.
        /// チェックディジット計算時の割り当て文字に対応する数値
        /// </summary>
        private static readonly int[] CHECKNUM = new int[]{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };

        /// <summary>
        /// constructor
        /// コンストラクタ
        /// </summary>
        /// <param name="zipcode">Zipcode 郵便番号</param>
        /// <param name="streetAddr">Addresses after town area names. 町域名以降の住所</param>
        /// <param name="direction">Drawing direction 作画方向</param>
        public JapanPostCB(string zipcode, string streetAddr, CodeDirection direction) : base("", direction)
        {
            TrackHeight = XUnit.FromMillimeter(1.2);
            AscenderHeight = XUnit.FromMillimeter(1.2);
            BarWidth = XUnit.FromMillimeter(0.6);
            BarSpace = XUnit.FromMillimeter(0.6);

            //Check zipcode
            if (zipcode == null) throw new ArgumentNullException("No zipcode description");
            if (zipcode.Length != 7) throw new ArgumentException("The zipcode is not seven digits");

            //Set the text to represent the barcode
            Text = zipcode + GenerateAddrDispNo(streetAddr);
        }

        /// <summary>
        /// constructor
        /// コンストラクタ
        /// </summary>
        /// <param name="zipcode">Zipcode 郵便番号</param>
        /// <param name="streetAddr">Addresses after town area names. 町域名以降の住所</param>
        public JapanPostCB(string zipcode, string streetAddr) : this(zipcode, streetAddr,CodeDirection.LeftToRight)
        {
        }

        /// <summary>
        /// Barcode level encoding
        /// バーコードレベルエンコード
        /// </summary>
        /// <param name="code">code</param>
        /// <returns>encoded code</returns>
        /// <exception cref="Exception">contains characters that can not be encoded</exception>
        protected override string[] EncodeHighLevel(String code)
        {
            var codewords = new System.Collections.ArrayList(code.Length);

            foreach (char c in code)
            {
                try
                {
                    codewords.Add(LINES[c]);
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception("Illegal character: " + c);
                }
            }

            return (string[])codewords.ToArray(typeof(string));
        }

        /// <summary>
        /// Calculate the check digit チェックデジット計算
        /// </summary>
        /// <param name="code">Code to calculate (20 padded)</param>
        /// <returns>Calculated check digit</returns>
        protected char CalcCheckdigit(string code)
        {
            int sum = 0;
            foreach (char c in code)
            {
                sum += CHECKNUM[BARCODECHR.IndexOf(c)];
            }

            int multiple = (sum / 19) * 19 + 19;
            int chkdigit = multiple - sum;
            return BARCODECHR[chkdigit];
        }

        /// <summary>
        /// Generate a barcode notation string
        /// バーコード表記文字列生成
        /// </summary>
        /// <param name="code">Code to generate</param>
        /// <returns>Generated code str</returns>
        protected string GenerateBarcodeStr(string code)
        {
            StringBuilder sb = new StringBuilder(40);
            foreach (char c in code)
            {
                // For the numbers
                // 数字の場合
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
                // For the alphabet
                // アルファベットの場合
                else if (ALPHABET.Contains(c))
                {
                    int AlphabetNo = (int)(c - 'A') + 10;
                    int NumericNo = (int)char.GetNumericValue(AlphabetNo.ToString()[0]) + 10;
                    sb.Append(BARCODECHR[NumericNo]);//CC1, CC2, CC3
                    sb.Append(AlphabetNo.ToString()[1]);
                }
                else
                {
                    switch (c)
                    {
                        case '-': sb.Append(BARCODECHR[10]); break;
                        case '$': sb.Append(BARCODECHR[14]); break;
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Check digit handling
        /// チェックデジットハンドリング
        /// </summary>
        /// <param name="code">code</param>
        /// <returns>Calculated code</returns>
        protected string HandleCheckdigit(string code)
        {
            return code + CalcCheckdigit(code).ToString();
        }

        /// <summary>
        /// Removing the start/stop character
        /// スタート・ストップキャラクターの除去
        /// </summary>
        /// <param name="code">code</param>
        /// <returns>Removed code</returns>
        private string RemoveStartStop(string code)
        {
            StringBuilder sb = new StringBuilder(code.Length);

            foreach (char c in code)
            {
                switch (c)
                {
                    case '(':
                    case '[':
                    case ')':
                    case ']':
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Create a barcode message
        /// バーコードメッセージ作成
        /// </summary>
        /// <param name="msg">message</param>
        /// <returns>created message</returns>
        protected override string CreateMessage(String msg)
        {
            //Remove start and stop characters
            String s = RemoveStartStop(msg);

            //20 digit padding (7 + 13 digits)
            //Generate barcode notation string
            s += new string('$', 20);
            s = GenerateBarcodeStr(s);
            s = s.Substring(0, 20);

            //grant a check digit
            s = HandleCheckdigit(s);

            //grant start and stop characters
            return "[" + s + "]";
        }

        /// <summary>
        /// Generate address display number from address after town name (street address),
        /// Kanji numerals are not supported.
        /// 町域名以降の住所から住所表記番号の生成　漢数字には対応しない
        /// </summary>
        /// <param name="streetAddr">address after town name (street address)</param>
        /// <returns>address display number</returns>
        private string GenerateAddrDispNo(String streetAddr)
        {
            //The generation rules are described below.
            //https://www.post.japanpost.jp/zipcode/zipmanual/p19.html

            String s = streetAddr;

            // Full-width half-width substitution in the specified range.
            // 指定範囲内の全角半角置換
            s = string.Join("", s.Select(n => (FULLWIDTH_HALFWIDTH.ContainsKey(n) ? FULLWIDTH_HALFWIDTH[n] : n)));

            // Replace all capital letters
            // 大文字置換
            s = s.ToUpper();

            // Remove a specified character
            // 指定文字除去
            s = string.Join("", s.Split('&', '＆', '/', '／', '･', '・', '.', '．'));

            // Extracts arithmetic digits, hyphens and one non-consecutive alphabetic character as required character information.
            // Substitute one hyphen for the following characters before the next extracted character.
            // Kanji,Kana,Katakana,Kanji numerals,Blank,Two or more consecutive alphabetic characters
            // 算用数字、ハイフンおよび連続していないアルファベット1文字を必要な文字情報として抜き出します。
            // 抜き出された文字の前にある下記の文字等は、ハイフン1文字に置き換えます。
            //  「漢字」、「かな文字」、「カタカナ文字」、「漢数字」、「ブランク」、「2文字以上連続したアルファベット文字」
            s = Regex.Replace(s, "[^0-9A-Z\\-]|[A-Z]{2,}", "-");

            //Trim Hyphen
            //ハイフントリム処理
            s = TrimHyphen(s);

            return s;
        }

        /// <summary>
        /// Trim Hyphen
        /// ハイフントリム処理
        /// </summary>
        /// <param name="target">target str 処理対象</param>
        /// <returns></returns>
        private static string TrimHyphen(string target)
        {
            // Make consecutive hyphens a single hyphen
            // 連続するハイフンを単一のハイフンにする
            target = Regex.Replace(target, "\\-{2,}", "-");

            // If number + 'F'
            // Remove if 'F' is a string terminator
            // 数字+「F」の場合
            //「F」が文字列終端である場合は除去する
            target = Regex.Replace(target, "([0-9])F$", "$1");
            // Otherwise, "F" converts to a hyphen
            // それ以外の場合、「F」はハイフンに変換する
            target = Regex.Replace(target, "([0-9])F", "$1-");

            // Remove hyphens before and after the alphabet
            // アルファベット前後のハイフンを除去
            var tmp = target.ToCharArray();
            for (int i = 0; i < target.Length; i++)
            {
                if (ALPHABET.Contains(target[i].ToString()))
                {
                    if (i > 0 && target[i - 1] == '-') tmp[i - 1] = '@';
                    if (i < target.Length - 1 && target[i + 1] == '-') tmp[i + 1] = '@';
                }
            }
            target = (new string(tmp)).Replace("@", "");

            // Remove leading hyphen
            // 先頭ハイフン除去            
            return target.Trim('-');
        }

        protected override void CheckCode(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
        }

    }
}
