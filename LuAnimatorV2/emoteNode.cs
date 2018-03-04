using System.Windows.Media.Imaging;

namespace LuAnimatorV2
{
    class emoteNode
    {
        public string name;

        public int speed;

        public bool looping;

        public BitmapSource[] frames;

        public BitmapSource[] fullbrightFrames;

        public string[] sound = null;

        public bool soundLoop;

        public double soundInterval;

        public double soundVolume = 1;

        public double soundPitch = 1;
    }
}
