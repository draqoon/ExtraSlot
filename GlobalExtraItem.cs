using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using TerraUI.Utilities;

namespace ExtraSlot {
    internal class GlobalExtraItem : GlobalItem {
        public override bool CanEquipAccessory( Item item, Player player, int slot ) {
            if( this.IsExtraAccessory( item ) ) {
                return (bool)ExtraSlot.Config.Get( ExtraSlot.AllowAccessorySlots );
            }

            if( this.IsFargowiltasSoulsAccessory( item ) ) {
                return (bool)ExtraSlot.Config.Get( ExtraSlot.AllowAccessorySlots );
            }

            return base.CanEquipAccessory( item, player, slot );
        }

        private bool IsExtraAccessory( Item item ) {
            return 0 < item.shoeSlot || 0 < item.shieldSlot || 0 < item.wingSlot;
        }

        private bool IsFargowiltasSoulsAccessory( Item item ) {
            var FargowiltasSouls = ModLoader.GetMod( "FargowiltasSouls" );
            if( FargowiltasSouls == null )
                return false;

            var player = Main.player[Main.myPlayer];
            var mp = player.GetModPlayer<ExtraSlotPlayer>( this.mod );

            if( mp.ConditionHandlerForFargowiltasSouls( item ) ) {
                return true;
            }

            return false;
        }

        public override bool CanRightClick( Item item ) {
            if( this.IsExtraAccessory( item ) || this.IsFargowiltasSoulsAccessory( item ) ) {
                return true;
            }
            return base.CanRightClick( item );
        }

        public override void RightClick( Item item, Player player ) {
            if( !this.CanRightClick( item ) ) {
                return;
            }

            var mp = player.GetModPlayer<ExtraSlotPlayer>( this.mod );

            var key = "";

            var FargowiltasSouls = ModLoader.GetMod( "FargowiltasSouls" );
            if( FargowiltasSouls != null ) {
                if( mp.ConditionHandlerForFargowiltasSouls( item ) ) {
                    key = ExtraSlotPlayer.FargowiltasSoulsKey;
                }
            }

            if( key == "" ) {
                if( 0 < item.shoeSlot ) {
                    key = ExtraSlotPlayer.ShoesKey;
                }
                else if( 0 < item.shieldSlot ) {
                    key = ExtraSlotPlayer.ShieldKey;
                }
                else if( 0 < item.wingSlot ) {
                    key = ExtraSlotPlayer.WingKey;
                }
            }

            if( key != "" ) {
                mp.Equip( key, KeyboardUtils.HeldDown( Keys.LeftShift ), item );
            }
            else {
                base.RightClick( item, player );
            }
        }
    }
}
