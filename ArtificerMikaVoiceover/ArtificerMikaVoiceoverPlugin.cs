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

namespace ArtificerMikaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Alicket.MisonoMikaArtificer")]
    [BepInPlugin("com.Schale.ArtificerMikaVoiceover", "ArtificerMikaVoiceover", "1.0.3")]
    public class ArtificerMikaVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        private static SurvivorDef mageSurvivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Merc/Mage.asset").WaitForCompletion(); //Why is this Merc?
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            BaseVoiceoverComponent.Init();
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
            bool foundSkin = false;
            SkinDef[] skins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("MageBody"));
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "MikaModDef")
                {
                    foundSkin = true;
                    ArtiMikaVoiceoverComponent.requiredSkinDefs.Add(skinDef);
                    break;
                }
            }

            if (!foundSkin)
            {
                Debug.LogError("ArtificerMikaVoiceover: Artificer Mika SkinDef not found. Voicelines will not work!");
            }
            else
            {
                On.RoR2.CharacterBody.Start += AttachVoiceoverComponent;

                On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
                {
                    orig(self);
                    if (self.currentSurvivorDef == mageSurvivorDef)
                    {
                        //Loadout isn't loaded first time this is called, so we need to manually get it.
                        //Probably not the most elegant way to resolve this.
                        if (self.loadoutDirty)
                        {
                            if (self.networkUser)
                            {
                                self.networkUser.networkLoadout.CopyLoadout(self.currentLoadout);
                            }
                        }

                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (ArtiMikaVoiceoverComponent.requiredSkinDefs.Contains(safe) && enableVoicelines.Value)
                        {
                            bool played = false;
                            if (!playedSeasonalVoiceline)
                            {
                                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                                {
                                    Util.PlaySound("Play_ArtiMika_Lobby_Newyear", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 5 && System.DateTime.Today.Day == 8)
                                {
                                    Util.PlaySound("Play_ArtiMika_Lobby_bday", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                                {
                                    Util.PlaySound("Play_ArtiMika_Lobby_Halloween", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                                {
                                    Util.PlaySound("Play_ArtiMika_Lobby_xmas", self.mannequinInstanceTransform.gameObject);
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
                                    Util.PlaySound("Play_ArtiMika_TitleDrop", self.mannequinInstanceTransform.gameObject);
                                }
                                else
                                {
                                    Util.PlaySound("Play_ArtiMika_Lobby", self.mannequinInstanceTransform.gameObject);
                                }
                            }
                        }
                    }
                };
            }
            ArtiMikaVoiceoverComponent.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");

            //Shrine Fail Hook
            On.RoR2.ShrineChanceBehavior.AddShrineStack += (orig, self, activator) =>
            {
                int successes = self.successfulPurchaseCount;
                orig(self, activator);

                //No change in successes = fail
                if (NetworkServer.active && self.successfulPurchaseCount == successes)
                {
                    if (activator)
                    {
                        ArtiMikaVoiceoverComponent vo = activator.GetComponent<ArtiMikaVoiceoverComponent>();
                        if (vo)
                        {
                            vo.PlayShrineOfChanceFailServer();
                        }
                    }
                }
            };

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

            nseList.Add(new NSEInfo(ArtiMikaVoiceoverComponent.nseBuffSelf));
            nseList.Add(new NSEInfo(ArtiMikaVoiceoverComponent.nseBlock));
            nseList.Add(new NSEInfo(ArtiMikaVoiceoverComponent.nseShrineFail));
            nseList.Add(new NSEInfo(ArtiMikaVoiceoverComponent.nseIceWall));
            RefreshNSE();
        }

        private void AttachVoiceoverComponent(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                if (ArtiMikaVoiceoverComponent.requiredSkinDefs.Contains(SkinCatalog.GetBodySkinDef(self.bodyIndex, (int)self.skinIndex)))
                {
                    BaseVoiceoverComponent existingVoiceoverComponent = self.GetComponent<BaseVoiceoverComponent>();
                    if (!existingVoiceoverComponent) self.gameObject.AddComponent<ArtiMikaVoiceoverComponent>();
                }
            }
        }

        private void InitNSE()
        {
            ArtiMikaVoiceoverComponent.nseBuffSelf = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            ArtiMikaVoiceoverComponent.nseBuffSelf.eventName = "Play_ArtiMika_BuffSelf";
            Content.networkSoundEventDefs.Add(ArtiMikaVoiceoverComponent.nseBuffSelf);

            ArtiMikaVoiceoverComponent.nseBlock = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            ArtiMikaVoiceoverComponent.nseBlock.eventName = "Play_ArtiMika_Blocked";
            Content.networkSoundEventDefs.Add(ArtiMikaVoiceoverComponent.nseBlock);

            ArtiMikaVoiceoverComponent.nseShrineFail = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            ArtiMikaVoiceoverComponent.nseShrineFail.eventName = "Play_ArtiMika_ShrineFail";
            Content.networkSoundEventDefs.Add(ArtiMikaVoiceoverComponent.nseShrineFail);

            ArtiMikaVoiceoverComponent.nseIceWall = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            ArtiMikaVoiceoverComponent.nseIceWall.eventName = "Play_ArtiMika_IceWall";
            Content.networkSoundEventDefs.Add(ArtiMikaVoiceoverComponent.nseIceWall);
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