using System;

namespace Yasin.EditorToolbox
{
    /// <summary>
    /// [Find] niteliği için arama kaynakları (Scene, Prefabs, Assets).
    /// </summary>
    [Flags]
    public enum FindSource
    {
        None    = 0,
        Scene   = 1 << 0,
        Prefabs = 1 << 1,
        Assets  = 1 << 2,

        Auto    = Scene | Prefabs | Assets
    }
}
