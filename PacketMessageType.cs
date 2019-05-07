namespace ExtraSlot {
    enum PacketMessageType : byte {
        EquipSlot = 1,
        VanitySlot = 2,
        DyeSlot = 4,
        All = EquipSlot | VanitySlot | DyeSlot,
        AllSlot = 0
    }
}
