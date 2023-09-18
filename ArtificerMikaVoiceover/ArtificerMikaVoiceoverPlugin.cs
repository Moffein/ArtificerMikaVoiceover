using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Audio;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using ArtificerMikaVoiceover.Components;
using ArtificerMikaVoiceover.Modules;
using System.Collections.Generic;
using BaseVoiceoverLib;

namespace ArtificerMikaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.BaseVoiceoverLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Alicket.MisonoMikaArtificer", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Schale.ArtificerMikaVoiceover", "ArtificerMikaVoiceover", "1.1.0")]
    public class ArtificerMikaVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        private static SurvivorDef mageSurvivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Merc/Mage.asset").WaitForCompletion(); //Why is this Merc?
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ArtificerMikaVoiceover.banditmikavoiceoverbundle"))  //Just reuse the icon from that one.
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Artificer Mika Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        private void Start()
        {
            SoundBanks.Init();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("texModIcon"));
        }

        private void OnLoad()
        {
            SkinDef mikaSkin = null;
            SkinDef[] skins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("MageBody"));
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "MikaModDef")
                {
                    mikaSkin = skinDef;
                    break;
                }
            }

            if (!mikaSkin)
            {
                Debug.LogError("ArtificerMikaVoiceover: Artificer Mika SkinDef not found. Voicelines will not work!");
            }
            else
            {
                VoiceoverInfo vo = new VoiceoverInfo(typeof(ArtiMikaVoiceoverComponent), mikaSkin, "MageBody");
                vo.selectActions += ArtiSelect;
            }

            On.EntityStates.Mage.Weapon.BaseChargeBombState.OnEnter += (orig, self) =>
            {
                orig(self);

                ArtiMikaVoiceoverComponent amvc = self.GetComponent<ArtiMikaVoiceoverComponent>();
                if (amvc)
                {
                    amvc.PlayBeginCharge();
                }
            };

            On.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += (orig, self) =>
            {
                orig(self);

                ArtiMikaVoiceoverComponent amvc = self.GetComponent<ArtiMikaVoiceoverComponent>();
                if (amvc)
                {
                    amvc.PlayReleaseCharge();
                }
            };

            RefreshNSE();
        }

        private void ArtiSelect(GameObject mannequinObject)
        {
            if (!enableVoicelines.Value) return;
            bool played = false;
            if (!playedSeasonalVoiceline)
            {
                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                {
                    Util.PlaySound("Play_ArtiMika_Lobby_Newyear", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 5 && System.DateTime.Today.Day == 8)
                {
                    Util.PlaySound("Play_ArtiMika_Lobby_bday", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                {
                    Util.PlaySound("Play_ArtiMika_Lobby_Halloween", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                {
                    Util.PlaySound("Play_ArtiMika_Lobby_xmas", mannequinObject);
                    played = true;
                }

                if (played)
                {
                    playedSeasonalVoiceline = true;
                }
            }
            if (!played)
            {
                if (Util.CheckRoll(5f))
                {
                    Util.PlaySound("Play_ArtiMika_TitleDrop", mannequinObject);
                }
                else
                {
                    Util.PlaySound("Play_ArtiMika_Lobby", mannequinObject);
                }
            }
        }

        private void InitNSE()
        {
            ArtiMikaVoiceoverComponent.nseBuffSelf = RegisterNSE("Play_ArtiMika_BuffSelf");
            ArtiMikaVoiceoverComponent.nseBlock = RegisterNSE("Play_ArtiMika_Blocked");
            ArtiMikaVoiceoverComponent.nseShrineFail = RegisterNSE("Play_ArtiMika_ShrineFail");
            ArtiMikaVoiceoverComponent.nseIceWall = RegisterNSE("Play_ArtiMika_IceWall");
        }

        private NetworkSoundEventDef RegisterNSE(string eventName)
        {
            NetworkSoundEventDef nse = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            nse.eventName = eventName;
            Content.networkSoundEventDefs.Add(nse);
            nseList.Add(new NSEInfo(nse));
            return nse;
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        public static List<NSEInfo> nseList = new List<NSEInfo>();
        public class NSEInfo
        {
            public NetworkSoundEventDef nse;
            public uint akId = 0u;
            public string eventName = string.Empty;

            public NSEInfo(NetworkSoundEventDef source)
            {
                this.nse = source;
                this.akId = source.akId;
                this.eventName = source.eventName;
            }

            private void DisableSound()
            {
                nse.akId = 0u;
                nse.eventName = string.Empty;
            }

            private void EnableSound()
            {
                nse.akId = this.akId;
                nse.eventName = this.eventName;
            }

            public void ValidateParams()
            {
                if (this.akId == 0u) this.akId = nse.akId;
                if (this.eventName == string.Empty) this.eventName = nse.eventName;

                if (!enableVoicelines.Value)
                {
                    DisableSound();
                }
                else
                {
                    EnableSound();
                }
            }
        }
    }
}