using BaseVoiceoverLib;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerMikaVoiceover.Components
{
    public class ArtiMikaVoiceoverComponent : BaseVoiceoverComponent
    {
        public static NetworkSoundEventDef nseBlock, nseShrineFail, nseIceWall, nseBuffSelf, nseTitle, nseIntro, nseHurt, nseOk, nseLaugh, nseOmoshiroi, nseMou, nseThanks, nseKocchi, nseIkuyo, nsePray1, nsePray2, nseProtect, nseExLevel1, nseExLevel2;

        private float levelCooldown = 0f;
        private float blockedCooldown = 0f;
        private float shrineOfChanceFailCooldown = 0f;
        private float utilityCooldown = 0f;
        private float specialCooldown = 0f;

        private bool acquiredScepter = false;

        protected override void Awake()
        {
            spawnVoicelineDelay = 3f;
            if (Run.instance && Run.instance.stageClearCount == 0)
            {
                spawnVoicelineDelay = 6.5f;
            }
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            if (inventory && inventory.GetItemCount(scepterIndex) > 0) acquiredScepter = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (levelCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
            if (shrineOfChanceFailCooldown > 0f) shrineOfChanceFailCooldown -= Time.fixedDeltaTime;
            if (blockedCooldown > 0f) blockedCooldown -= Time.fixedDeltaTime;
            if (utilityCooldown > 0f) utilityCooldown -= Time.fixedDeltaTime;
            if (specialCooldown > 0f) specialCooldown -= Time.fixedDeltaTime;
        }

        public override void PlayDamageBlockedServer()
        {
            if (!NetworkServer.active || blockedCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseBlock, 2.2f, false);
            if (played) blockedCooldown = 30f;
        }

        public override void PlayDeath()
        {
            TryPlaySound("Play_ArtiMika_Defeat", 4.5f, true);
        }

        public override void PlayHurt(float percentHPLost)
        {
            if (percentHPLost >= 0.1f)
            {
                TryPlaySound("Play_ArtiMika_TakeDamage", 0f, false);
            }
        }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played = TryPlaySound("Play_ArtiMika_LevelUp", 10f, false);
            if (played) levelCooldown = 60f;
        }


        public override void PlaySpawn()
        {
            TryPlaySound("Play_ArtiMika_Spawn", 3f, true);
        }

        public override void PlaySpecialAuthority(GenericSkill skill)
        {
            if (specialCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseBuffSelf, 1.7f, false);
            if (played) specialCooldown = 20f;
        }

        public override void PlayTeleporterFinish()
        {
            TryPlaySound("Play_ArtiMika_Victory", 4f, false);
        }

        public override void PlayTeleporterStart()
        {
            TryPlaySound("Play_ArtiMika_TeleporterStart", 4.2f, false);
        }

        public override void PlayUtilityAuthority(GenericSkill skill)
        {
            if (utilityCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseIceWall, 1.8f, false);
            if (played) utilityCooldown = 20f;
        }

        public override void PlayVictory()
        {
            TryPlaySound("Play_ArtiMika_Victory", 4f, true);
        }

        protected override void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            base.Inventory_onItemAddedClient(itemIndex);
            if (scepterIndex != ItemIndex.None && itemIndex == scepterIndex)
            {
                PlayAcquireScepter();
            }
            else
            {
                ItemDef id = ItemCatalog.GetItemDef(itemIndex);
                if (itemIndex == RoR2Content.Items.Squid.itemIndex || itemIndex == RoR2Content.Items.Plant.itemIndex)
                {
                    PlayBadItem();
                }
                else if (id && id.deprecatedTier == ItemTier.Tier3)
                {
                    PlayAcquireLegendary();
                }
            }
        }

        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_ArtiMika_AcquireScepter", 19f, true);
            acquiredScepter = true;
        }

        public void PlayAcquireLegendary()
        {
            TryPlaySound("Play_ArtiMika_Relationship", 9f, false);
        }

        public void PlayBadItem()
        {
            TryPlaySound("Play_ArtiMika_Cafe2", 4.2f, false);
        }

        public override void PlayShrineOfChanceFailServer()
        {
            if (!NetworkServer.active || shrineOfChanceFailCooldown > 0f) return;
            if (Util.CheckRoll(15f))
            {
                bool played = TryPlayNetworkSound(nseShrineFail, 2.7f, false);
                if (played) shrineOfChanceFailCooldown = 60f;
            }
        }

        //Called via hook
        public void PlayBeginCharge()
        {
            TryPlaySound("Play_ArtiMika_Buffed", 0f, false);
        }

        //Called via hook
        public void PlayReleaseCharge()
        {
            TryPlaySound("Play_ArtiMika_Shout", 0f, false);
        }

        protected override void CheckInputs()
        {
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonTitle))
            {
                TryPlayNetworkSound(nseTitle, 1.63f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonIntro))
            {
                TryPlayNetworkSound(nseIntro, 15.6f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonOk))
            {
                TryPlayNetworkSound(nseOk, 0.75f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonKocchi))
            {
                TryPlayNetworkSound(nseKocchi, 1.64f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonOmoshiroi))
            {
                TryPlayNetworkSound(nseOmoshiroi, 4f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonMou))
            {
                TryPlayNetworkSound(nseMou, 2.65f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonLaugh))
            {
                TryPlayNetworkSound(nseLaugh, 0.5f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonMuri))
            {
                TryPlayNetworkSound(nseBlock, 1.95f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonThanks))
            {
                TryPlayNetworkSound(nseThanks, 0.6f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonHurt))
            {
                TryPlayNetworkSound(nseHurt, 0.1f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonIkuyo))
            {
                TryPlayNetworkSound(nseIkuyo, 0.7f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonPray))
            {
                if (Util.CheckRoll(50f))
                {
                    TryPlayNetworkSound(nsePray1, 4f, false);
                }
                else
                {
                    TryPlayNetworkSound(nsePray2, 3.15f, false);
                }
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonProtect))
            {
                TryPlayNetworkSound(nseProtect, 1.57f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(ArtificerMikaVoiceoverPlugin.buttonPass))
            {
                if (Util.CheckRoll(50f))
                {
                    TryPlayNetworkSound(nseExLevel1, 1.45f, false);
                }
                else
                {
                    TryPlayNetworkSound(nseExLevel2, 1.75f, false);
                }
                return;
            }
        }

        public override bool ComponentEnableVoicelines()
        {
            return ArtificerMikaVoiceoverPlugin.enableVoicelines.Value;
        }
    }
}
