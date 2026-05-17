using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace IT_HelpDesk.Data.Classes
{
    public static class AvatarHelper
    {
        public static string GetAvatarsDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "IT-HelpDesk", "Avatars");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetFullAvatarPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (relativePath.StartsWith("/Data/Images/"))
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath.TrimStart('/'));
            return Path.Combine(GetAvatarsDirectory(), relativePath);
        }

        public static BitmapImage LoadImage(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return null;
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public static BitmapImage GetDefaultAvatar()
        {
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Images", "avatar.jpg");
            return LoadImage(defaultPath) ?? null;
        }

    }
}
