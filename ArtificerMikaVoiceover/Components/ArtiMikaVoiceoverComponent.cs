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
        public static List<SkinDef> requiredSkinDefs = new List<SkinDef>();
        public static ItemIndex ScepterIndex;

        public static NetworkSoundEventDef nseBlock;
        public static NetworkSoundEventDef nseShrineFail;
        public static NetworkSoundEventDef nseIceWall;
        public static NetworkSoundEventDef nseBuffSelf;
        //public static NetworkSoundEventDef nseCommonSkill;
        //public static NetworkSoundEventDef nseTactical;

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
            if (inventory && inventory.GetItemCount(ScepterIndex) > 0) acquiredScepter = true;
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

        public override void PlayJump() { }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played = TryPlaySound("Play_ArtiMika_LevelUp", 10f, false);
            if (played) levelCooldown = 60f;
        }

        public override void PlayLowHealth() { }

        public override void PlayPrimaryAuthority() { }

        public override void PlaySecondaryAuthority() {}

        public override void PlaySpawn()
        {
            TryPlaySound("Play_ArtiMika_Spawn", 3f, true);
        }

        public override void PlaySpecialAuthority()
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

        public override void PlayUtilityAuthority()
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
            if (ArtiMikaVoiceoverComponent.ScepterIndex != ItemIndex.None && itemIndex == ArtiMikaVoiceoverComponent.ScepterIndex)
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

        //Called via hook
        public void PlayShrineOfChanceFailServer()
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
    }
}
