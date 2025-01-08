using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicketTrackingTool
{
    public static class AssetManager
    {
        // Navigate to the project root dynamically
        private static readonly string assetsBasePath = Path.Combine(
            Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName,
            "Assets"
        );

        public static string AssetsBasePath => assetsBasePath;

        public static string WacogImagePath => Path.Combine(assetsBasePath, "WACOG.png");

        public static Image LoadImage(string assetName)
        {
            string fullPath = Path.Combine(assetsBasePath, assetName);
            if (File.Exists(fullPath))
            {
                return Image.FromFile(fullPath);
            }
            else
            {
                throw new FileNotFoundException($"Asset not found at path: {fullPath}");
            }
        }

        public static void SetPictureBox(PictureBox pictureBox, string imagePath)
        {
            pictureBox.Image = LoadImage(imagePath);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        }
    }
}
