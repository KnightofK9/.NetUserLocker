using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FileLockWPF.design
{
    class ImageInfo
    {
        private String _Title;

        public string Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        private String _ImagePath;
        private BitmapImage _ImageData;
        public BitmapImage ImageData
        {
            get { return this._ImageData; }
            set { this._ImageData = value; }
        }

        public string ImagePath { get => _ImagePath; set => _ImagePath = value; }
    }
}
