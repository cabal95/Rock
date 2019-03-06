﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
#if !IS_NET_CORE
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
#endif
using System.IO;
using System.Web;
#if IS_NET_CORE
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.MetaData;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
#else
using Goheer.EXIF;
#endif

namespace Rock.Utility
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileUtilities
    {
        /// <summary>
        /// Gets the file bytes.
        /// </summary>
        /// <param name="uploadedFile">The uploaded file.</param>
        /// <param name="resizeIfImage">if set to <c>true</c> [resize if image].</param>
        /// <returns></returns>
#if IS_NET_CORE
        public static Stream GetFileContentStream( Microsoft.AspNetCore.Http.IFormFile uploadedFile, bool resizeIfImage = true )
#else
        public static Stream GetFileContentStream( HttpPostedFile uploadedFile, bool resizeIfImage = true )
#endif
        {
            if ( uploadedFile.ContentType == "image/svg+xml" || uploadedFile.ContentType == "image/tiff" || !uploadedFile.ContentType.StartsWith( "image/" ) )
            {
#if IS_NET_CORE
                return uploadedFile.OpenReadStream();
#else
                return uploadedFile.InputStream;
#endif
            }

            try
            {
#if IS_NET_CORE
                var image = Image.Load( uploadedFile.OpenReadStream() );
                var orientation = image.MetaData.ExifProfile?.GetValue( SixLabors.ImageSharp.MetaData.Profiles.Exif.ExifTag.Orientation );
                if ( orientation != null )
                {
                    // EFTODO: Implement the rotation mutation.
                }

                if ( resizeIfImage )
                {
                    image.Mutate( a =>
                    {
                        a = a.Resize( new ResizeOptions { Mode = ResizeMode.Max, Size = new Size( 1024, 768 ) } );
                    } );
                }

                var stream = new MemoryStream();
                image.Save( stream, ContentTypeToImageFormat( uploadedFile.ContentType ) );
                return stream;
#else
                var bmp = new Bitmap( uploadedFile.InputStream );

                // Check to see if we should flip the image.
                var exif = new EXIFextractor( ref bmp, "\n" );
                if ( exif["Orientation"] != null )
                {
                    var flip = OrientationToFlipType( exif["Orientation"].ToString() );

                    // don't flip if orientation is correct
                    if ( flip != RotateFlipType.RotateNoneFlipNone )
                    {
                        bmp.RotateFlip( flip );
                        exif.setTag( 0x112, "1" ); // reset orientation tag
                    }
                }

                if ( resizeIfImage )
                {
                    bmp = RoughResize( bmp, 1024, 768 );
                }

                var stream = new MemoryStream();
                bmp.Save( stream, ContentTypeToImageFormat( uploadedFile.ContentType ) );
                return stream;
#endif
            }
            catch
            {
                // if it couldn't be converted to a bitmap or if the exif or resize thing failed, just return the original stream
#if IS_NET_CORE
                return uploadedFile.OpenReadStream();
#else
                return uploadedFile.InputStream;
#endif
            }
        }

        /// <summary>
        /// Returns the ImageFormat for the given ContentType string.
        /// Throws NotSupportedException if given an unknown/unsupported content type.
        /// </summary>
        /// <param name="contentType">the content type</param>
        /// <returns>ImageFormat</returns>
#if IS_NET_CORE
        private static IImageFormat ContentTypeToImageFormat( string contentType )
        {
            switch ( contentType )
            {
                case "image/jpg":
                case "image/jpeg":
                    return SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance;

                case "image/png":
                    return SixLabors.ImageSharp.Formats.Png.PngFormat.Instance;

                case "image/gif":
                    return SixLabors.ImageSharp.Formats.Gif.GifFormat.Instance;

                case "image/bmp":
                    return SixLabors.ImageSharp.Formats.Bmp.BmpFormat.Instance;

                default:
                    throw new NotSupportedException( string.Format( "unknown ImageFormat for {0}", contentType ) );
            }
        }
#else
        private static ImageFormat ContentTypeToImageFormat( string contentType )
        {
            switch ( contentType )
            {
                case "image/jpg":
                case "image/jpeg":
                    return ImageFormat.Jpeg;

                case "image/png":
                    return ImageFormat.Png;

                case "image/gif":
                    return ImageFormat.Gif;

                case "image/bmp":
                    return ImageFormat.Bmp;

                case "image/tiff":
                    return ImageFormat.Tiff;

                default:
                    throw new NotSupportedException( string.Format( "unknown ImageFormat for {0}", contentType ) );
            }
        }
#endif

#if !IS_NET_CORE
        // EFTODO: Implement this.

        /// <summary>
        /// Orientations the type of to flip.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        /// <returns></returns>
        private static RotateFlipType OrientationToFlipType( string orientation )
        {
            switch ( int.Parse( orientation ) )
            {
                case 1:
                    return RotateFlipType.RotateNoneFlipNone;

                case 2:
                    return RotateFlipType.RotateNoneFlipX;

                case 3:
                    return RotateFlipType.Rotate180FlipNone;

                case 4:
                    return RotateFlipType.Rotate180FlipX;

                case 5:
                    return RotateFlipType.Rotate90FlipX;

                case 6:
                    return RotateFlipType.Rotate90FlipNone;

                case 7:
                    return RotateFlipType.Rotate270FlipX;

                case 8:
                    return RotateFlipType.Rotate270FlipNone;

                default:
                    return RotateFlipType.RotateNoneFlipNone;
            }
        }
#endif

#if !IS_NET_CORE
        /// <summary>
        /// Roughes the resize.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <returns></returns>
        private static Bitmap RoughResize( Bitmap input, int maxWidth, int maxHeight )
        {
            // ensure resize is even needed
            if ( input.Width > maxWidth || input.Height > maxHeight )
            {
                // determine which is dimension difference is larger
                if ( ( input.Width - maxWidth ) > ( input.Height - maxHeight ) )
                {
                    // width difference is larger
                    double resizeRatio = maxWidth / ( double ) input.Width;
                    int newHeight = Convert.ToInt32( input.Height * resizeRatio );
                    input = ( Bitmap ) ResizeImage( ( Image ) input, new Size( maxWidth, newHeight ) );
                }
                else
                {
                    double resizeRatio = maxHeight / ( double ) input.Height;
                    int newWidth = Convert.ToInt32( input.Width * resizeRatio );
                    input = ( Bitmap ) ResizeImage( ( Image ) input, new Size( newWidth, maxHeight ) );
                }
            }

            return input;
        }

        /// <summary>
        /// Resizes the image.
        /// </summary>
        /// <param name="imgToResize">The img to resize.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        private static Image ResizeImage( Image imgToResize, Size size )
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercentW = ( float ) size.Width / ( float ) sourceWidth;
            float nPercentH = ( float ) size.Height / ( float ) sourceHeight;

            float nPercent = ( nPercentH < nPercentW ) ? nPercentH : nPercentW;

            int destWidth = ( int ) ( sourceWidth * nPercent );
            int destHeight = ( int ) ( sourceHeight * nPercent );

            Bitmap b = new Bitmap( destWidth, destHeight );
            Graphics g = Graphics.FromImage( ( Image ) b );
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage( imgToResize, 0, 0, destWidth, destHeight );
            g.Dispose();

            return ( Image ) b;
        }
#endif
    }
}