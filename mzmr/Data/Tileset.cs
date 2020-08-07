﻿using mzmr.Items;
using System;

namespace mzmr.Data
{
    public class Tileset
    {
        private Palette palette;
        private TileTable tileTable;
        private byte[] animTileset;

        private readonly ROM rom;
        private readonly int addr;
        private readonly byte number;

        // TODO: clean up everything!
        public Tileset(ROM rom, byte tsNum)
        {
            this.rom = rom;
            addr = rom.TilesetOffset + tsNum * 0x14;
            number = tsNum;

            palette = new Palette(rom, addr + 4, 14);
            tileTable = new TileTable(rom, addr + 0xC);

            // get animTileset
            byte atsNum = rom.Read8(addr + 0x10);
            int atsOffset = rom.AnimTilesetOffset + atsNum * 0x30;
            animTileset = new byte[0x30];
            rom.RomToArray(animTileset, atsOffset, 0, 0x30);
        }

        public byte AddAbility(ItemType item)
        {
            byte animGfxNum = (byte)(ROM.NumOfAnimGfx + item - ItemType.Long);

            // find empty spot in palette
            int palRow = 15;
            for (int r = 1; r < 14; r++)
            {
                ushort color = palette.GetColor(r, 1);
                bool blank = true;

                for (int c = 2; c < 16; c++)
                {
                    if (palette.GetColor(r, c) != color)
                    {
                        blank = false;
                        break;
                    }
                }

                if (blank)
                {
                    Palette itemPal = item.AbilityPalette();
                    palette.CopyRows(itemPal, 0, r, 1);
                    palRow = r + 2;
                    break;
                }
            }

            // find empty spot in animGfx
            int animGfxSlot = 0;
            for (int i = 0; i < 0x10; i++)
            {
                if (animTileset[i * 3] == 0)
                {
                    animTileset[i * 3] = animGfxNum;
                    animGfxSlot = i;
                    break;
                }
            }

            // find empty spot in tile table
            int blockNum = 0x4C;
            int tileVal = 0x40;
            for (int i = 0x4C; i < 0x50; i++)
            {
                int offset = i * 4 + 1;

                if (tileTable[offset] == 0x40 && tileTable[offset + 1] == 0x40 &&
                    tileTable[offset + 2] == 0x40 && tileTable[offset + 3] == 0x40)
                {
                    tileVal = (palRow << 12) | (animGfxSlot * 4);
                    tileTable[offset] = (ushort)tileVal;
                    tileTable[offset + 1] = (ushort)(tileVal + 1);
                    tileTable[offset + 2] = (ushort)(tileVal + 2);
                    tileTable[offset + 3] = (ushort)(tileVal + 3);
                    blockNum = i;
                    break;
                }
            }

            // fix TileTable400
            int ttb400Offset = rom.TileTable400Offset + (0xD0 + item - ItemType.Long) * 8;
            rom.Write16(ttb400Offset, (ushort)tileVal);
            rom.Write16(ttb400Offset + 2, (ushort)(tileVal + 1));
            rom.Write16(ttb400Offset + 4, (ushort)(tileVal + 2));
            rom.Write16(ttb400Offset + 6, (ushort)(tileVal + 3));

            return (byte)blockNum;
        }

        public void Write(byte tsNum)
        {
            if (tsNum == number)
            {
                // replace
                // write palette
                palette.Write();

                // write tile table
                tileTable.Write();

                // write animTileset
                byte atsNum = rom.Read8(addr + 0x10);
                int atsOffset = rom.AnimTilesetOffset + atsNum * 0x30;
                rom.ArrayToRom(animTileset, 0, atsOffset, 0x30);                
            }
            else
            {
                // copy
                int newAddr = rom.TilesetOffset + tsNum * 0x14;

                // copy levelGfx pointer
                int levelGfxOffset = rom.ReadPtr(addr);
                rom.WritePtr(newAddr, levelGfxOffset);

                // write palette
                palette.WriteCopy(newAddr + 4);

                // copy BG3gfx pointer
                int BG3gfxOffset = rom.ReadPtr(addr + 8);
                rom.WritePtr(newAddr + 8, BG3gfxOffset);

                // write tile table
                tileTable.WriteCopy(newAddr + 0xC);

                // write animTileset
                int diff = tsNum - ROM.NumOfTilesets;
                byte atsNum = (byte)(ROM.NumOfAnimTilesets + diff);
                int atsOffset = rom.AnimTilesetOffset + atsNum * 0x30;
                rom.ArrayToRom(animTileset, 0, atsOffset, 0x30);
                rom.Write8(newAddr + 0x10, atsNum);

                // copy animPalette number
                byte apNum = rom.Read8(addr + 0x11);
                rom.Write8(newAddr + 0x11, apNum);
            }
        }

    }
}
