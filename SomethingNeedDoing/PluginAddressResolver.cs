﻿using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;

namespace SomethingNeedDoing
{
    internal delegate IntPtr FrameworkGetUiModuleDelegate(IntPtr basePtr);
    internal delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

    internal class PluginAddressResolver : BaseAddressResolver
    {
        public IntPtr FrameworkGetUIModuleAddress { get; private set; }
        public IntPtr FrameworkPointerAddress { get; private set; }
        public IntPtr ProcessChatBoxAddress { get; private set; }
        public IntPtr EventFrameworkAddress { get; private set; }

        private const string FrameworkGetUIModuleSignature = "E8 ?? ?? ?? ?? 48 83 7F ?? 00 48 8B F0";  // Client::System::Framework::Framework.GetUIModule
        private const string FrameworkPointerSignature = "48 8B 0D ?? ?? ?? ?? 48 8D 54 24 ?? 48 83 C1 10 E8 ?? ?? ?? ??";  // g_Framework2
        private const string ProcessChatBoxSignature = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";
        private const string EventFrameworkSignature = "40 53 48 83 EC 20 32 DB 48 83 3D ?? ?? ?? ?? ??";  // g_EventFramework

        protected override void Setup64Bit(SigScanner scanner)
        {
            FrameworkGetUIModuleAddress = scanner.ScanText(FrameworkGetUIModuleSignature);
            FrameworkPointerAddress = scanner.GetStaticAddressFromSig(FrameworkPointerSignature);
            ProcessChatBoxAddress = scanner.ScanText(ProcessChatBoxSignature);
            EventFrameworkAddress = scanner.GetStaticAddressFromSig(EventFrameworkSignature) + 1;

            PluginLog.Verbose("===== SOMETHING NEED DOING =====");
            PluginLog.Verbose($"{nameof(FrameworkGetUIModuleAddress)} {FrameworkGetUIModuleAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(FrameworkPointerAddress)} {FrameworkPointerAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(ProcessChatBoxAddress)} {ProcessChatBoxAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(EventFrameworkAddress)} {EventFrameworkAddress.ToInt64():X}");
        }
    }

}
