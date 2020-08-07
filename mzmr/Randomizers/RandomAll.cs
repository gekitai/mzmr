﻿using mzmr.Data;
using mzmr.Items;
using mzmr.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace mzmr.Randomizers
{
    public class RandomAll
    {
        private readonly ROM rom;
        private readonly Settings settings;
        private readonly int seed;

        private RandomItems randItems;
        private RandomEnemies randEnemies;
        private RandomPalettes randPals;

        public RandomAll(ROM rom, Settings settings, int seed)
        {
            this.rom = rom;
            this.settings = settings;
            this.seed = seed;
        }

        public bool Randomize()
        {
            // allow palettes to be randomized separately
            Random rng = new Random(seed);

            // randomize palette
            randPals = new RandomPalettes(rom, settings, rng);
            randPals.Randomize();

            rng = new Random(seed);

            // randomize items
            randItems = new RandomItems(rom, settings, rng);
            bool success = randItems.Randomize();
            if (!success) { return false; }

            // randomize enemies
            randEnemies = new RandomEnemies(rom, settings, rng);
            randEnemies.Randomize();

            ApplyTweaks();
            DrawFileSelectHash();
            WriteVersion();

            return true;
        }

        private void ApplyTweaks()
        {
            // always add underscore character
            Patch.Apply(rom, Properties.Resources.ZM_U_underscore);

            if (settings.iceNotRequired)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_metroidIce);
            }
            if (settings.plasmaNotRequired)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_blackPiratePlasma);
            }
            if (settings.enableItemToggle)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_itemToggle);
            }
            if (settings.obtainUnkItems)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_unkItems);
            }
            if (settings.hardModeAvailable)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_hardModeAvailable);
            }
            if (settings.pauseScreenInfo)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_pauseScreenInfo);
            }
            if (settings.removeCutscenes)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_removeCutscenes);
            }
            if (settings.skipSuitless)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_skipSuitless);
            }
            if (settings.skipDoorTransitions)
            {
                Patch.Apply(rom, Properties.Resources.ZM_U_skipDoorTransitions);
            }
        }

        private void DrawFileSelectHash()
        {
            // TODO: add a tweak to copy the whole palette
            string s = settings.GetString() + seed;
            int hash = s.GetHashCode();

            const int palPtr = 0x7C7CC;
            const int gfxPtr = 0x7C7E0;
            const int ttbPtr = 0x7C80C;

            // get palette, graphics, and tile table
            Palette filePal = new Palette(rom, palPtr, 7);
            GFX fileGfx = new GFX(rom, gfxPtr, 32);
            TileTable fileTtb = new TileTable(rom, ttbPtr, true);

            for (int i = 0; i < 4; i++)
            {
                int index = hash & 15;
                hash >>= 4;
                ItemType item = (index + ItemType.Super);

                // modify palette
                filePal.AppendPalette(item.AbilityPalette());

                // modify graphics
                GFX itemGfx = item.AbilityGraphics();
                Rectangle rect = new Rectangle(0, 0, 2, 2);
                fileGfx.AddGfx(itemGfx, rect, i * 3, 17);

                // modify tile table
                int x = 9 + i * 3;
                int pal = i + 7;
                fileTtb.SetPalette(pal, x, 1);
                fileTtb.SetPalette(pal, x, 2);
                fileTtb.SetPalette(pal, x + 1, 1);
                fileTtb.SetPalette(pal, x + 1, 2);
                fileTtb.SetTileNumber(0, x + 2, 1);
                fileTtb.SetTileNumber(0, x + 2, 2);
            }

            // write palette, graphics, and tile table
            filePal.Write();
            fileGfx.Write();
            fileTtb.Write();

            // TODO: make patch
            // mov r3,0xA0
            rom.Write16(0x7C69E, 0x23A0);
            // lsl r4,r3,0x13
            rom.Write16(0x7C6A0, 0x04DC);
            // add r3,0xC0
            rom.Write16(0x7C6A8, 0x33C0);
        }

        private void WriteVersion()
        {
            // MZM Randomizer v2.0.0
            // Seed: <seed>
            // Settings: <settings>
            string config = settings.GetString();
            string text = $"MZM Randomizer v{Program.Version}\nSeed: {seed}\nSettings: {config}\n";
            byte[] values = Text.BytesFromText(text);
            rom.ArrayToRom(values, 0, ROM.InfoOffset, values.Length);
        }

        public string GetLog()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Seed: {seed}");
            sb.AppendLine($"Settings: {settings.GetString()}");
            sb.AppendLine();

            sb.AppendLine(randItems.GetLog());
            sb.AppendLine(randEnemies.GetLog());
            sb.AppendLine(randPals.GetLog());

            return sb.ToString();
        }

        public Bitmap[] GetMaps()
        {
            return randItems.GetMaps();
        }

        public static void ShuffleList<T>(Random rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }

    }
}
