using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using MelonLoader;
using Harmony;
using System.Linq;

namespace OGTrustRanks
{
    public static class BuildInfo
    {
        public const string Name = "OGTrustRanks";
        public const string Author = "Herp Derpinstine, Emilia, and dave-kun";
        public const string Company = "Lava Gang";
        public const string Version = "1.0.7";
        public const string DownloadLink = "https://github.com/HerpDerpinstine/OGTrustRanks";
    }

    public class OGTrustRanks : MelonMod
    {
        private static PropertyInfo VRCPlayer_ModTag = null;
        private static Color TrustedUserColor;
        private static Color VeteranUserColor;
        private static Color LegendaryUserColor;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("ogtrustranks", "OGTrustRanks");
            MelonPreferences.CreateEntry("ogtrustranks", "enabled", true, "Enabled");
            MelonPreferences.CreateEntry("ogtrustranks", "VeteranColorR", 171, "Red component of the Veteran color");
            MelonPreferences.CreateEntry("ogtrustranks", "VeteranColorG", 205, "Green component of the Veteran color");
            MelonPreferences.CreateEntry("ogtrustranks", "VeteranColorB", 239, "Blue component of the Veteran color");
            MelonPreferences.CreateEntry("ogtrustranks", "LegendaryColorR", 255, "Red component of the Legendary color");
            MelonPreferences.CreateEntry("ogtrustranks", "LegendaryColorG", 105, "Green component of the Legendary color");
            MelonPreferences.CreateEntry("ogtrustranks", "LegendaryColorB", 180, "Blue component of the Legendary color");

            int VeteranR = MelonPreferences.GetEntryValue<int>("ogtrustranks", "VeteranColorR");
            int VeteranG = MelonPreferences.GetEntryValue<int>("ogtrustranks", "VeteranColorG");
            int VeteranB = MelonPreferences.GetEntryValue<int>("ogtrustranks", "VeteranColorB");
            int LegendaryR = MelonPreferences.GetEntryValue<int>("ogtrustranks", "LegendaryColorR");
            int LegendaryG = MelonPreferences.GetEntryValue<int>("ogtrustranks", "LegendaryColorG");
            int LegendaryB = MelonPreferences.GetEntryValue<int>("ogtrustranks", "LegendaryColorB");

            TrustedUserColor = new Color(0.5058824f, 0.2627451f, 0.9019608f);
            VeteranUserColor = new Color(VeteranR / 255.0f, VeteranG / 255.0f, VeteranB / 255.0f);
            LegendaryUserColor = new Color(LegendaryR / 255.0f, LegendaryG / 255.0f, LegendaryB / 255.0f);

            var FriendlyNameTargetMethod = typeof(VRCPlayer).GetMethods().Where(it => !it.Name.Contains("PDM") && it.ReturnType.ToString().Equals("System.String") && it.GetParameters().Length == 1 && it.GetParameters()[0].ParameterType.ToString().Equals("VRC.Core.APIUser")).FirstOrDefault();
            Harmony.Patch(FriendlyNameTargetMethod, new HarmonyMethod(typeof(OGTrustRanks).GetMethod("GetFriendlyDetailedNameForSocialRank", BindingFlags.NonPublic | BindingFlags.Static)));

            var ColorForRankTargetMethods = typeof(VRCPlayer).GetMethods().Where(it => it.ReturnType.ToString().Equals("UnityEngine.Color") && it.GetParameters().Length == 1 && it.GetParameters()[0].ParameterType.ToString().Equals("VRC.Core.APIUser")).ToList();
            ColorForRankTargetMethods.ForEach(it =>
                Harmony.Patch(it, new HarmonyMethod(typeof(OGTrustRanks).GetMethod("GetColorForSocialRank", BindingFlags.NonPublic | BindingFlags.Static)))
            );
        }

        public override void OnPreferencesSaved()
        {
            int VeteranR = MelonPreferences.GetEntryValue<int>("ogtrustranks", "VeteranColorR");
            int VeteranG = MelonPreferences.GetEntryValue<int>("ogtrustranks", "VeteranColorG");
            int VeteranB = MelonPreferences.GetEntryValue<int>("ogtrustranks", "VeteranColorB");
            int LegendaryR = MelonPreferences.GetEntryValue<int>("ogtrustranks", "LegendaryColorR");
            int LegendaryG = MelonPreferences.GetEntryValue<int>("ogtrustranks", "LegendaryColorG");
            int LegendaryB = MelonPreferences.GetEntryValue<int>("ogtrustranks", "LegendaryColorB");

            VeteranUserColor = new Color(VeteranR / 255.0f, VeteranG / 255.0f, VeteranB / 255.0f);
            LegendaryUserColor = new Color(LegendaryR / 255.0f, LegendaryG / 255.0f, LegendaryB / 255.0f);

            SetupTrustRankButton();
        }

        public override void OnLevelWasInitialized(int level) => SetupTrustRankButton();

        private static void SetupTrustRankButton()
        {
            if (QuickMenu.prop_QuickMenu_0 != null)
            { 
                GameObject QuickMenu_gameObject = QuickMenu.prop_QuickMenu_0.field_Private_GameObject_4;
                if (QuickMenu_gameObject != null)
                {
                    UiToggleButton component = QuickMenu_gameObject.transform.Find("Toggle_States_ShowTrustRank_Colors").GetComponent<UiToggleButton>();
                    if (component != null)
                    {
                        bool is_enabled = MelonPreferences.GetEntryValue<bool>("ogtrustranks", "enabled");
                        if (is_enabled)
                        {
                            TrustRanks rank = GetTrustRankEnum(APIUser.CurrentUser);
                            if (rank == TrustRanks.VETERAN)
                                SetupRankDisplay(component, "Veteran User", VeteranUserColor);
                            else if (rank == TrustRanks.LEGENDARY)
                                SetupRankDisplay(component, "Legendary User", LegendaryUserColor);
                        }
                        else
                            SetupRankDisplay(component, "Trusted User", TrustedUserColor);
                    }
                }
            }
        }

        private static void SetupRankDisplay(UiToggleButton toggleButton, string display_name, Color color)
        {
            Transform displayTransform = toggleButton.transform.Find("TRUSTED");
            if (displayTransform != null)
            {
                GameObject gameObject = displayTransform.gameObject;
                if ((gameObject != null) && (gameObject.gameObject != null))
                {
                    toggleButton.field_Public_GameObject_0 = gameObject.transform.Find("ON").gameObject;
                    Text[] btnTextsOn = toggleButton.field_Public_GameObject_0.GetComponentsInChildren<Text>();
                    btnTextsOn[3].text = display_name;
                    btnTextsOn[3].color = color;
                    toggleButton.field_Public_GameObject_1 = gameObject.transform.Find("OFF").gameObject;
                    Text[] btnTextsOff = toggleButton.field_Public_GameObject_1.GetComponentsInChildren<Text>();
                    btnTextsOff[3].text = display_name;
                    btnTextsOff[3].color = color;
                }
            }
        }

        private static bool GetFriendlyDetailedNameForSocialRank(APIUser __0, ref string __result)
        {
            if ((__0 != null) && MelonPreferences.GetEntryValue<bool>("ogtrustranks", "enabled"))
            {
                Player player = GetUserByID(__0.id);
                if (!__0.hasVIPAccess || (__0.hasModerationPowers && ((!(null != player) || !(null != player.field_Internal_VRCPlayer_0) ? !__0.showModTag : string.IsNullOrEmpty((string)VRCPlayer_ModTag.GetGetMethod().Invoke(player.field_Internal_VRCPlayer_0, null))))))
                {
                    TrustRanks rank = GetTrustRankEnum(__0);
                    if (rank == TrustRanks.LEGENDARY)
                    {
                        __result = "Legendary User";
                        return false;
                    }
                    else if (rank == TrustRanks.VETERAN)
                    {
                        __result = "Veteran User";
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool GetColorForSocialRank(APIUser __0, ref Color __result)
        {
            if ((__0 != null) && MelonPreferences.GetEntryValue<bool>("ogtrustranks", "enabled") && !APIUser.IsFriendsWith(__0.id))
            {
                Player player = GetUserByID(__0.id);
                if (!__0.hasVIPAccess || (__0.hasModerationPowers && ((!(null != player) || !(null != player.field_Internal_VRCPlayer_0) ? !__0.showModTag : string.IsNullOrEmpty((string)VRCPlayer_ModTag.GetGetMethod().Invoke(player.field_Internal_VRCPlayer_0, null))))))
                {
                    TrustRanks rank = GetTrustRankEnum(__0);
                    if (rank == TrustRanks.LEGENDARY)
                    {
                        __result = LegendaryUserColor;
                        return false;
                    }
                    else if (rank == TrustRanks.VETERAN)
                    {
                        __result = VeteranUserColor;
                        return false;
                    }
                }
            }
            return true;
        }

        private static TrustRanks GetTrustRankEnum(APIUser user)
        {
            if ((user != null) && (user.tags != null) && (user.tags.Count > 0))
            {
                if (user.tags.Contains("system_legend") && user.tags.Contains("system_trust_legend") && user.tags.Contains("system_trust_trusted"))
                    return TrustRanks.LEGENDARY;
                else if (user.tags.Contains("system_trust_legend") && user.tags.Contains("system_trust_trusted"))
                    return TrustRanks.VETERAN;
            }
            return TrustRanks.IGNORE;
        }

        private enum TrustRanks
        {
            IGNORE,
            VETERAN,
            LEGENDARY
        }

        private static Player GetUserByID(string userID)
        {
            foreach (Player ply in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if ((ply.prop_APIUser_0 != null) && (ply.prop_APIUser_0.id == userID))
                    return ply;
            return null;
        }
    }
}
