using System;
using System.Linq;
using System.Reflection;
using ExitGames.Client.Photon;
using MelonLoader;
using UnhollowerRuntimeLib.XrefScans;
using VRC;
using VRC.Core;
using UIExpansionKit.API;
using VRC.DataModel;
using UnityEngine;

[assembly: MelonGame("VRChat")]
[assembly: MelonInfo(typeof(LocalClone.Main), nameof(LocalClone), "0.1.0", " Astrum-Project & elian")]
[assembly: MelonColor(ConsoleColor.DarkYellow)]
[assembly: MelonAdditionalDependencies("UIExpansionKit")]

namespace LocalClone
{
    public class Main : MelonMod
    {
        public static bool State { get; set; } = false;
        public static MelonLogger.Instance Logger;
        public override void OnApplicationStart()
        {
            #region Init
            Logger = LoggerInstance;
            _onEvent = Helper.Patch<OnEvent>(
                typeof(VRCNetworkingClient).GetMethod(nameof(VRCNetworkingClient.OnEvent)),
                typeof(Main).GetMethod(nameof(Main.Detour), BindingFlags.Static | BindingFlags.NonPublic)    
            );
            _loadAvatarMethod = //to my knowledge this is from loukylor's VRChatUtilityKit or sum shit
                typeof(VRCPlayer).GetMethods()
                .First(mi =>
                    mi.Name.StartsWith("Method_Private_Void_Boolean_")
                    && mi.Name.Length < 31
                    && mi.GetParameters().Any(pi => pi.IsOptional)
                    && XrefScanner.UsedBy(mi) // Scan each method
                        .Any(instance => instance.Type == XrefType.Method
                            && instance.TryResolve() != null
                            && instance.TryResolve().Name == "ReloadAvatarNetworkedRPC"));
#endregion
            #region UIXSupport
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu)
                .AddSimpleButton(
                    "Local Clone", 
                    new Action(LocalCloneSelected)
                );
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu)
                .AddToggleButton(
                    "Local Clone State",
                    new Action<bool>((b) => { State = b; ReloadAvatar(); }),
                    null
                );
            #endregion
        }
        public override void OnUpdate()
        {
            if (!Input.anyKey) return;
            if (!Input.GetKey(KeyCode.Tab)) return;
            if (!Input.GetKeyDown(KeyCode.T)) return;
            LocalCloneSelected();
        }
        private static void LocalCloneSelected()
        {
            var usr = UserSelectionManager.field_Private_Static_UserSelectionManager_0.field_Private_APIUser_1;
            if (usr == null)
            {
                Logger.Msg("Target is null.");
                return;
            }
            AvatarDictCache = PlayerManager.prop_PlayerManager_0
                .field_Private_List_1_Player_0
                .ToArray()
                .FirstOrDefault(a => a.field_Private_APIUser_0.id == usr.id)
                ?.prop_Player_1.field_Private_Hashtable_0["avatarDict"];
            ReloadAvatar();
        }
        private static IntPtr Detour(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr)
        {
            if (!State) return _onEvent(instancePtr, eventDataPtr, nativeMethodInfoPtr);
            try
            {
                var data = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<EventData>(eventDataPtr);
                if (data.Code != 42) return _onEvent(instancePtr, eventDataPtr, nativeMethodInfoPtr);
                if (AvatarDictCache != null
                    && data.Sender == Player.prop_Player_0.field_Private_VRCPlayerApi_0.playerId
                ) data.CustomData.Cast<Il2CppSystem.Collections.Hashtable>()["avatarDict"] = AvatarDictCache;
            }
            catch { Logger.Msg("Something exploded during " + nameof(Main.Detour)); }
            return _onEvent(instancePtr, eventDataPtr, nativeMethodInfoPtr);
        }
        private delegate IntPtr OnEvent(IntPtr instancePtr, IntPtr eventDataPtr, IntPtr nativeMethodInfoPtr);
        private static OnEvent _onEvent;
        private static Il2CppSystem.Object AvatarDictCache { get; set; }
        private static MethodInfo _loadAvatarMethod;
        private static void ReloadAvatar() => _loadAvatarMethod.Invoke(VRCPlayer.field_Internal_Static_VRCPlayer_0, new object[] { true });

    }
}
