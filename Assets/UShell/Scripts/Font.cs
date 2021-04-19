using UnityEngine;

namespace UShell
{
    internal enum FontType
    {
        BUILTIN,
        RESOURCE,
        FILE
    }
    internal class UFont
    {
        public readonly FontType fontType;
        public readonly Font font;

        public UFont(FontType fontType, Font font)
        {
            this.fontType = fontType;
            this.font = font;
        }
        public void Free()
        {
            if (fontType == FontType.RESOURCE)
                Resources.UnloadAsset(font);
            else if (fontType == FontType.FILE)
                Font.Destroy(font);
        }
    }
}
