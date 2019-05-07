using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace ExtraSlot {
    public class ExtraSlot : Mod {
        public const string AllowAccessorySlots = "allowInAccessorySlots";
        public const string AllowFargoAccessorySlots = "allowFargoInAccessorySlots";
        public static readonly ModConfig Config = new ModConfig( nameof( ExtraSlot ) );

        public override void Load() {
            this.Properties = new ModProperties() {
                Autoload = true,
                AutoloadBackgrounds = true,
                AutoloadSounds = true
            };

            Config.Add( AllowAccessorySlots, false );
            Config.Add( AllowFargoAccessorySlots, false );
            Config.Load();
        }

        public override void Unload() {
        }

        public override void ModifyInterfaceLayers( List<GameInterfaceLayer> layers ) {
            if( Main.gameMenu )
                return;

            var layer = new LegacyGameInterfaceLayer( $"{nameof( ExtraSlot )}: DrawItemSlot", () => {
                var wsp = Main.player[Main.myPlayer].GetModPlayer<ExtraSlotPlayer>( this );
                wsp.Draw( Main.spriteBatch );

                return true;
            }, InterfaceScaleType.UI );

            var index = layers.FindIndex( x => x.Name == "Vanilla: Inventory" );
            if( -1 < index ) {
                layers.Insert( index, layer );
            }
            else {
                layers.Add( layer );
            }
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            var index = reader.ReadByte();
            var message = (PacketMessageType)reader.ReadByte();
            var player = reader.ReadByte();
            var modPlayer = Main.player[player].GetModPlayer<ExtraSlotPlayer>();

            try {
                switch( message ) {
                    case PacketMessageType.AllSlot: {
                        var count = (int)reader.ReadByte();
                        foreach( var group in modPlayer.Slots ) {
                            group.EquipSlot.Item = ItemIO.Receive( reader );
                            group.VanitySlot.Item = ItemIO.Receive( reader );
                            group.DyeSlot.Item = ItemIO.Receive( reader );
                        }
                        if( Main.netMode == 2 ) {
                            var packet = GetPacket();
                            packet.Write( (byte)PacketMessageType.AllSlot );
                            packet.Write( player );
                            packet.Write( (byte)count );
                            foreach( var group in modPlayer.Slots ) {
                                ItemIO.Send( group.EquipSlot.Item, packet );
                                ItemIO.Send( group.VanitySlot.Item, packet );
                                ItemIO.Send( group.DyeSlot.Item, packet );
                            }
                            packet.Send( -1, whoAmI );
                        }
                        break;
                    }
                    case PacketMessageType.All: {
                        var group = modPlayer.Slots[index];
                        group.EquipSlot.Item = ItemIO.Receive( reader );
                        group.VanitySlot.Item = ItemIO.Receive( reader );
                        group.DyeSlot.Item = ItemIO.Receive( reader );
                        if( Main.netMode == 2 ) {
                            var packet = GetPacket();
                            packet.Write( index );
                            packet.Write( (byte)PacketMessageType.All );
                            packet.Write( player );
                            ItemIO.Send( group.EquipSlot.Item, packet );
                            ItemIO.Send( group.VanitySlot.Item, packet );
                            ItemIO.Send( group.DyeSlot.Item, packet );
                            packet.Send( -1, whoAmI );
                        }
                        break;
                    }
                    case PacketMessageType.EquipSlot: {
                        var group = modPlayer.Slots[index];
                        group.EquipSlot.Item = ItemIO.Receive( reader );
                        if( Main.netMode == 2 ) {
                            modPlayer.SendSingleItemPacket( index, PacketMessageType.EquipSlot, group.EquipSlot.Item, -1, whoAmI );
                        }
                        break;
                    }
                    case PacketMessageType.VanitySlot: {
                        var group = modPlayer.Slots[index];
                        group.VanitySlot.Item = ItemIO.Receive( reader );
                        if( Main.netMode == 2 ) {
                            modPlayer.SendSingleItemPacket( index, PacketMessageType.VanitySlot, group.VanitySlot.Item, -1, whoAmI );
                        }
                        break;
                    }
                    case PacketMessageType.DyeSlot: {
                        var group = modPlayer.Slots[index];
                        group.DyeSlot.Item = ItemIO.Receive( reader );
                        if( Main.netMode == 2 ) {
                            modPlayer.SendSingleItemPacket( index, PacketMessageType.DyeSlot, group.DyeSlot.Item, -1, whoAmI );
                        }
                        break;
                    }
                    default:
                        ErrorLogger.Log( $"{nameof( ExtraSlot )}.{nameof( HandlePacket )}: Unknown message type: {message}" );
                        break;
                }
            }
            catch( Exception ex ) {
                ErrorLogger.Log( $"{nameof( ExtraSlot )}.{nameof( HandlePacket )}: {ex}" );
            }
        }
    }
}
