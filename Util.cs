using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KB30
{
    class Util
    {
        public static BitmapImage BitmapFromUri(Uri uri, int decode_pixel_height = 0)
        {
            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.BeginInit();
                bmp.UriSource = uri;
                if (decode_pixel_height != 0)
                {
                    bmp.DecodePixelHeight = decode_pixel_height;
                }
                bmp.EndInit();
                bmp.Freeze();
            }
            catch (Exception)
            {
                BitmapImage bmp2 = new BitmapImage();
                try
                {
                    bmp2.BeginInit();
                    bmp2.UriSource = new Uri(@"pack://application:,,,/Resources/error.png", UriKind.Absolute);
                    if (decode_pixel_height != 0)
                    {
                        bmp2.DecodePixelHeight = decode_pixel_height;
                    }
                    bmp2.EndInit();
                    bmp2.Freeze();
                    return bmp2;
                }
                catch (Exception e2)
                {
                    MessageBox.Show("Error loading image file: " + uri.ToString(), "Call the doctor, I think I'm gonna crash!");
                    return null;
                }
            }
            return bmp;
        }


    }
}
