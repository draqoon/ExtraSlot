using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using TerraUI;
using TerraUI.Objects;

namespace ExtraSlot {

    internal class ExtraSlotPlayer : ModPlayer {

        public class SlotGroup {

            public SlotGroup( string key, string hoverText, DrawHandler drawItem, DrawHandler drawBackground, DrawHandler drawBackgroundDye, ConditionHandler conditionHandler ) {
                this.Key = key;

                this.EquipSlot = new UIItemSlot( Vector2.Zero, 
                                                 context: ItemSlot.Context.EquipAccessory, 
                                                 hoverText: hoverText,
                                                 conditions: conditionHandler,
                                                 drawItem: drawItem,
                                                 drawBackground: drawBackground,
                                                 scaleToInventory: true );
                this.VanitySlot = new UIItemSlot( Vector2.Zero, 
                                                  context: ItemSlot.Context.EquipAccessoryVanity, 
                                                  hoverText: Language.GetTextValue( "LegacyInterface.11" ) + " " + hoverText,
                                                  conditions: conditionHandler,
                                                  drawItem: drawItem,
                                                  drawBackground: drawBackground, 
                                                  scaleToInventory: true );
                this.DyeSlot = new UIDyeItemSlot( Vector2.Zero, 
                                                  context: ItemSlot.Context.EquipDye, 
                                                  conditions: item => item.dye > 0 && item.hairDye < 0,
                                                  drawItem: drawItem,
                                                  drawBackground: drawBackgroundDye, 
                                                  scaleToInventory: true );
                this.VanitySlot.Partner = this.EquipSlot;
                this.EquipSlot.BackOpacity = this.VanitySlot.BackOpacity = this.DyeSlot.BackOpacity = .8f;
            }

            public string HiddenTag => $"{this.Key}_hidden";

            public string EquipTag => $"{this.Key}_equip";
            public string VanityTag => $"{this.Key}_vanity";
            public string DyeTag => $"{this.Key}_dye";

            public string Key { get; }

            public UIItemSlot EquipSlot { get; }
            public UIItemSlot VanitySlot { get; }
            public UIItemSlot DyeSlot { get; }

        }

        public List<SlotGroup> Slots = new List<SlotGroup>();

        public const string ShoesKey = "shoes";
        public const string ShieldKey = "shield";
        public const string WingKey = "wing";

        #region FargowiltasSouls 専用
        public const string FargowiltasSoulsKey = "FargowiltasSouls";
        #endregion

        public override void clientClone(ModPlayer clientClone) {
            var clone = clientClone as ExtraSlotPlayer;

            if(clone == null) {
                return;
            }

            for( var i = 0; i < this.Slots.Count; i++ ) {
                clone.Slots[i].EquipSlot.Item = this.Slots[i].EquipSlot.Item.Clone();
                clone.Slots[i].VanitySlot.Item = this.Slots[i].VanitySlot.Item.Clone();
                clone.Slots[i].DyeSlot.Item = this.Slots[i].DyeSlot.Item.Clone();
            }
        }

        public override void SendClientChanges(ModPlayer clientPlayer) {
            var oldClone = clientPlayer as ExtraSlotPlayer;

            if(oldClone == null) {
                return;
            }

            for( var i = 0; i < this.Slots.Count; i++ ) {
                var group = this.Slots[i];
                var oldGropu = oldClone.Slots[i];

                if( oldGropu.EquipSlot.Item.IsNotTheSameAs( group.EquipSlot.Item ) ) {
                    SendSingleItemPacket( i, PacketMessageType.EquipSlot, group.EquipSlot.Item, -1, this.player.whoAmI );
                }

                if( oldGropu.VanitySlot.Item.IsNotTheSameAs( group.VanitySlot.Item ) ) {
                    SendSingleItemPacket( i, PacketMessageType.VanitySlot, group.VanitySlot.Item, -1, this.player.whoAmI );
                }

                if( oldGropu.DyeSlot.Item.IsNotTheSameAs( group.DyeSlot.Item ) ) {
                    SendSingleItemPacket( i, PacketMessageType.DyeSlot, group.DyeSlot.Item, -1, this.player.whoAmI );
                }
            }
        }

        internal void SendSingleItemPacket( int index, PacketMessageType message, Item item, int toWho, int fromWho) {
            var packet = this.mod.GetPacket();
            packet.Write( (byte)index );
            packet.Write( (byte)message );
            packet.Write( (byte)player.whoAmI );
            ItemIO.Send( item, packet );
            packet.Send( toWho, fromWho );
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            var packet = this.mod.GetPacket();
            packet.Write( (byte)255 );
            packet.Write((byte)PacketMessageType.AllSlot);
            packet.Write((byte)player.whoAmI);
            packet.Write( (byte)this.Slots.Count );
            for( var i = 0; i < this.Slots.Count; i++ ) {
                var group = this.Slots[i];
                ItemIO.Send( group.EquipSlot.Item, packet );
                ItemIO.Send( group.VanitySlot.Item, packet );
                ItemIO.Send( group.DyeSlot.Item, packet );
            }
            packet.Send(toWho, fromWho);
        }

        /// <summary>
        /// Initialize the ModPlayer.
        /// </summary>
        public override void Initialize() {

            this.Slots = new List<SlotGroup>();

            {
                ConditionHandler conditionHandler = item => 0 < item.shoeSlot || 0 < item.shieldSlot || 0 < item.wingSlot;
                this.Slots.Add( new SlotGroup( 
                                    ShoesKey, 
                                    "Shoes",
                                    ( sender, spriteBatch ) => this.DrawItem( sender, spriteBatch, null ),
                                    ( sender, spriteBatch ) => this.DrawSlotBackground( sender, spriteBatch, ItemID.HermesBoots ), 
                                    this.DyeSlot_DrawBackground, 
                                    conditionHandler ) );
                this.Slots.Add( new SlotGroup( 
                                    ShieldKey, 
                                    "Shield",
                                    ( sender, spriteBatch ) => this.DrawItem( sender, spriteBatch, null ),
                                    ( sender, spriteBatch ) => this.DrawSlotBackground( sender, spriteBatch, ItemID.CobaltShield ), 
                                    this.DyeSlot_DrawBackground, 
                                    conditionHandler ) );
                this.Slots.Add( new SlotGroup( 
                                    WingKey, 
                                    "Wing",
                                    ( sender, spriteBatch ) => this.DrawItem( sender, spriteBatch, null ),
                                    ( sender, spriteBatch ) => this.DrawSlotBackground( sender, spriteBatch, ItemID.AngelWings ), 
                                    this.DyeSlot_DrawBackground, 
                                    conditionHandler ) );
            }

            var FargowiltasSouls = ModLoader.GetMod( "FargowiltasSouls" );
            if( FargowiltasSouls != null ) {

                //ErrorLogger.Log( $"{nameof( ExtraSlotPlayer )}.{nameof( Initialize )}: {FargowiltasSouls.Name} is Loaded." );

                this.ConditionHandlerForFargowiltasSouls = item => this.IsFargowiltasSoulsAccessory( FargowiltasSouls, FargowiltasSouls.ItemType( "EternitySoul" ), item.type );
                this.Slots.Add( new SlotGroup(
                                    FargowiltasSoulsKey,
                                    "Soul of Eternity",
                                    ( sender, spriteBatch ) => this.DrawItem( sender, spriteBatch, null ),
                                    ( sender, spriteBatch ) => this.DrawSlotBackground( sender, spriteBatch, FargowiltasSouls.ItemType( "EternitySoul" ) ),
                                    this.DyeSlot_DrawBackground,
                                    this.ConditionHandlerForFargowiltasSouls ) );
            }

            InitializeSlots();
        }

        private bool IsFargowiltasSoulsAccessory( Mod FargowiltasSouls, int rootItemType, int checkingItemType, HashSet<int> checkedItemTypes = null ) {

            if( rootItemType == checkingItemType ) {
                return true;
            }

            if( checkedItemTypes == null ) {
                checkedItemTypes = new HashSet<int>();
            }
            checkedItemTypes.Add( rootItemType );

            var recipes = Main.recipe.Where( r => r.createItem.type == rootItemType );

            foreach( var recipe in recipes ) {

                var requiredItems = recipe.requiredItem.Where( x => x.accessory && x.modItem != null && x.modItem.mod == FargowiltasSouls );

                foreach( var requiredItem in requiredItems ) {
                    if( checkedItemTypes.Contains( requiredItem.type ) )
                        continue;

                    var r = this.IsFargowiltasSoulsAccessory( FargowiltasSouls, requiredItem.type, checkingItemType, checkedItemTypes );
                    if( r ) {
                        return true;
                    }
                }
            }

            return false;
        }

        public ConditionHandler ConditionHandlerForFargowiltasSouls { get; private set; }

        public override void ModifyDrawInfo(ref PlayerDrawInfo drawInfo) {
            for( var i = 0; i < this.Slots.Count; i++ ) {
                var group = this.Slots[i];

                if( group.DyeSlot.Item.stack > 0 && ( group.EquipSlot.Item.wingSlot > 0 || group.VanitySlot.Item.wingSlot > 0 ) ) {
                    drawInfo.wingShader = group.DyeSlot.Item.dye;
                }
                if( group.DyeSlot.Item.stack > 0 && ( group.EquipSlot.Item.shoeSlot > 0 || group.VanitySlot.Item.shoeSlot > 0 ) ) {
                    drawInfo.shoeShader = group.DyeSlot.Item.dye;
                }
                if( group.DyeSlot.Item.stack > 0 && ( group.EquipSlot.Item.shieldSlot > 0 || group.VanitySlot.Item.shieldSlot > 0 ) ) {
                    drawInfo.shieldShader = group.DyeSlot.Item.dye;
                }
            }
        }

        /// <summary>
        /// Update player with the equipped wings.
        /// </summary>
        public override void UpdateEquips(ref bool wallSpeedBuff, ref bool tileSpeedBuff, ref bool tileRangeBuff) {
            for( var i = 0; i < this.Slots.Count; i++ ) {
                var group = this.Slots[i];

                var item = group.EquipSlot.Item;
                var vanity = group.VanitySlot.Item;

                if( item.stack > 0 ) {
                    this.player.VanillaUpdateAccessory( player.whoAmI, item, !group.EquipSlot.ItemVisible, ref wallSpeedBuff, ref tileSpeedBuff, ref tileRangeBuff );
                    this.player.VanillaUpdateEquip( item );
                }

                if( vanity.stack > 0 ) {
                    this.player.VanillaUpdateVanityAccessory( vanity );
                }
            }
        }

        /// <summary>
        /// Since there is no tModLoader hook in UpdateDyes, we use PreUpdateBuffs which is right after that.
        /// </summary>
        public override void PreUpdateBuffs() {
            for( var i = 0; i < this.Slots.Count; i++ ) {
                var group = this.Slots[i];

                if( group.DyeSlot.Item != null && !group.EquipSlot.Item.IsAir && group.EquipSlot.ItemVisible ) {
                    if( group.EquipSlot.Item.shoeSlot > 0 )
                        this.player.cShoe = group.DyeSlot.Item.dye;
                    if( group.EquipSlot.Item.shieldSlot > 0 )
                        this.player.cShield = group.DyeSlot.Item.dye;
                    if( group.EquipSlot.Item.wingSlot > 0 )
                        this.player.cWings = group.DyeSlot.Item.dye;
                }
                if( group.DyeSlot.Item != null && !group.VanitySlot.Item.IsAir ) {
                    if( group.VanitySlot.Item.shoeSlot > 0 )
                        this.player.cShoe = group.DyeSlot.Item.dye;
                    if( group.VanitySlot.Item.shieldSlot > 0 )
                        this.player.cShield = group.DyeSlot.Item.dye;
                    if( group.VanitySlot.Item.wingSlot > 0 )
                        this.player.cWings = group.DyeSlot.Item.dye;
                }
            }
        }

        /// <summary>
        /// Save the mod settings.
        /// </summary>
        public override TagCompound Save() {
            var tag = new TagCompound();
            foreach( var group in this.Slots ) {
                tag.Add( group.HiddenTag, group.EquipSlot.ItemVisible );
                tag.Add( group.EquipTag, ItemIO.Save( group.EquipSlot.Item ) );
                tag.Add( group.VanityTag, ItemIO.Save( group.VanitySlot.Item ) );
                tag.Add( group.DyeTag, ItemIO.Save( group.DyeSlot.Item ) );
            }
            return tag;
        }

        /// <summary>
        /// Load the mod settings.
        /// </summary>
        public override void Load(TagCompound tag) {
            foreach( var group in this.Slots ) {
                group.EquipSlot.Item = ItemIO.Load( tag.GetCompound( group.EquipTag ) ).Clone();
                group.VanitySlot.Item = ItemIO.Load( tag.GetCompound( group.VanityTag ) ).Clone();
                group.DyeSlot.Item = ItemIO.Load( tag.GetCompound( group.DyeTag ) ).Clone();
                group.EquipSlot.ItemVisible = tag.GetBool( group.HiddenTag );
            }
        }

        private void DrawItem( UIObject sender, SpriteBatch spriteBatch, Item backgroundItem = null ) {
            var slot = (UIItemSlot)sender;
            int type;
            if( backgroundItem  == null ) {
                type = slot.Item.type;
            }
            else {
                type = backgroundItem.type;
            }
            if( type == 0 )
                return;

            var texture = Main.itemTexture[type];

            Rectangle rect;
            if( Main.itemAnimations[type] != null ) {
                rect = Main.itemAnimations[type].GetFrame( texture );
            }
            else {
                rect = texture.Frame( 1, 1, 0, 0 );
            }

            var scale = Main.inventoryScale;
            if( 32 < rect.Width || 32 < rect.Height ) {
                if( rect.Height < rect.Width ) {
                    scale *= 32f / rect.Width;
                }
                else {
                    scale *= 32f / rect.Height;
                }
            }

            var origin = rect.Size() / 2f;
            var position = new Rectangle( 
                                slot.Rectangle.X,
                                slot.Rectangle.Y, 
                                (int)( slot.Rectangle.Width * Main.inventoryScale ),
                                (int)( slot.Rectangle.Height * Main.inventoryScale )
                           ).Center.ToVector2();

            var opacity = backgroundItem != null ? 0.25f : slot.ItemOpacity;

            spriteBatch.Draw(
                texture,
                position,
                new Rectangle?( rect ),
                Color.White * opacity,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                0f );
        }

        /// <summary>
        /// Draw the wing slot backgrounds.
        /// </summary>
        private void DrawSlotBackground( UIObject sender, SpriteBatch spriteBatch, int backgroundImageItem ) {
            var slot = (UIItemSlot)sender;

            if( ShouldDrawSlots() ) {
                slot.OnDrawBackground( spriteBatch );

                if( slot.Item.stack == 0 ) {
                    var item = new Item();
                    item.SetDefaults( backgroundImageItem, true );
                    this.DrawItem( sender, spriteBatch, item );
                }
            }
        }

        /// <summary>
        /// Control what can be placed in the wing slots.
        /// </summary>
        private static bool WingSlot_Conditions(Item item) {
            if(item.wingSlot <= 0) {
                return false;
            }

            return true;
        }

        private static bool ExtraSlot_Conditions( Item item ) {
            if( item.wingSlot <= 0 && item.shoeSlot <= 0 && item.shieldSlot <= 0 ) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Draw the wing dye slot background.
        /// </summary>
        private void DyeSlot_DrawBackground( UIObject sender, SpriteBatch spriteBatch ) {
            UIItemSlot slot = (UIItemSlot)sender;

            if(!ShouldDrawSlots()) {
                return;
            }

            slot.OnDrawBackground(spriteBatch);
            
            if(slot.Item.stack != 0) {
                return;
            }

            Texture2D tex = Main.extraTexture[54];
            Rectangle rectangle = tex.Frame(3, 6, 1 % 3);
            rectangle.Width -= 2;
            rectangle.Height -= 2;
            Vector2 origin = rectangle.Size() / 2f * Main.inventoryScale;
            Vector2 position = slot.Rectangle.TopLeft();

            spriteBatch.Draw(
                tex,
                position + (slot.Rectangle.Size() / 2f) - (origin / 2f),
                rectangle,
                Color.White * 0.35f,
                0f,
                origin,
                Main.inventoryScale,
                SpriteEffects.None,
                0f); // layer depth 0 = front
        }

        /// <summary>
        /// Draw the wing slots.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public void Draw(SpriteBatch spriteBatch) {
            if(!ShouldDrawSlots()) {
                return;
            }

            int mapH = 0;
            float origScale = Main.inventoryScale;

            Main.inventoryScale = 0.85f;

            if( Main.mapEnabled && !Main.mapFullscreen && Main.mapStyle == 1 ) {
                mapH = 256;
            }

            if( Main.mapEnabled ) {
                int adjustY = 600;

                if( Main.player[Main.myPlayer].ExtraAccessorySlotsShouldShow ) {
                    adjustY = 610 + PlayerInput.UsingGamepad.ToInt() * 30;
                }

                if( ( mapH + adjustY ) > Main.screenHeight ) {
                    mapH = Main.screenHeight - adjustY;
                }
            }

            int slotCount = 7 + Main.player[Main.myPlayer].extraAccessorySlots;

            if( ( Main.screenHeight < 900 ) && ( slotCount >= 8 ) ) {
                slotCount = 7;
            }

            var rX = Main.screenWidth - 92 - 14 - ( 47 * 3 ) - (int)( Main.extraTexture[58].Width * Main.inventoryScale );
            var rY = (int)( mapH + 174 + 4 + slotCount * 56 * Main.inventoryScale );

            for( var i = this.Slots.Count - 1; 0 <= i; i-- ) {
                //ErrorLogger.Log( $"{nameof(ExtraSlotPlayer)}.{nameof(Draw)}: [{i}] {this.Slots[i].Key}" );

                var x = rX;
                var y = rY;
                rY -= 47;

                var group = this.Slots[i];
                group.EquipSlot.Position = new Vector2( x, y );
                group.EquipSlot.Draw( spriteBatch );

                group.VanitySlot.Position = new Vector2( x - 47, y );
                group.VanitySlot.Draw( spriteBatch );

                group.DyeSlot.Position = new Vector2( x - 47 * 2, y );
                group.DyeSlot.Draw( spriteBatch );
            }

            Main.inventoryScale = origScale;

            foreach( var group in this.Slots ) {
                group.EquipSlot.Update();
                group.VanitySlot.Update();
                group.DyeSlot.Update();
            }
        }

        /// <summary>
        /// Whether to draw the UIItemSlots.
        /// </summary>
        /// <returns>whether to draw the slots</returns>
        private static bool ShouldDrawSlots() {
            return Main.playerInventory && Main.EquipPage == 0;
        }

        /// <summary>
        /// Initialize the items in the UIItemSlots.
        /// </summary>
        private void InitializeSlots() {
            foreach( var group in this.Slots ) {
                group.EquipSlot.Item = new Item();
                group.EquipSlot.Item.SetDefaults( 0, true ); // Can remove "0, true" once 0.10.1.5 comes out.

                group.VanitySlot.Item = new Item();
                group.VanitySlot.Item.SetDefaults( 0, true );

                group.DyeSlot.Item = new Item();
                group.DyeSlot.Item.SetDefaults( 0, true );
            }
        }

        /// <summary>
        /// Equip a set of wings.
        /// </summary>
        /// <param name="isVanity">whether the wings should go in the vanity slot</param>
        /// <param name="item">wings</param>
        public void Equip( string key, bool isVanity, Item item ) {

            var group = this.Slots.SingleOrDefault( x => x.Key == key );
            if( group == null )
                return;

            var slot = ( isVanity ? group.VanitySlot : group.EquipSlot );
            var fromSlot = Array.FindIndex( this.player.inventory, i => i == item );

            // from inv to slot
            if( fromSlot < 0 ) {
                return;
            }

            item.favorited = false;
            this.player.inventory[fromSlot] = slot.Item.Clone();
            Main.PlaySound( SoundID.Grab );
            Recipe.FindRecipes();
            slot.Item = item.Clone();
        }

        /// <summary>
        /// Equip a dye.
        /// </summary>
        /// <param name="item">dye to equip</param>
        public void EquipDye( string key, Item item) {
            var group = this.Slots.SingleOrDefault( x => x.Key == key );
            if( group == null )
                return;

            var fromSlot = Array.FindIndex( this.player.inventory, i => i == item );

            // from inv to slot
            if( fromSlot < 0) {
                return;
            }

            item.favorited = false;
            this.player.inventory[fromSlot] = group.DyeSlot.Item.Clone();
            Main.PlaySound( SoundID.Grab );
            Recipe.FindRecipes();
            group.DyeSlot.Item = item.Clone();
        }
        
    }
}
