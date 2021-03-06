﻿using AmongUsMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cheat
{
    class Memory
    {
        static int UPDATE_DELAY = 500;

        #region States
        static List<AmongUsMemory.Player> playerDatas = new List<AmongUsMemory.Player>();
        static bool fakeImp = false;
        #endregion

        public static void Init()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Update();
                    System.Threading.Thread.Sleep(UPDATE_DELAY);
                }
            });

            // Handle Console Commands
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    string input = Console.ReadLine().ToLower();
                    RunCommand(input);
                }
            });

            Console.Clear();
            Console.WriteLine("Initialized cheat.");
        }

        public static void Reset()
        {
            foreach (AmongUsMemory.Player p in playerDatas) p.StopObserveState();
            playerDatas = AmongUsMemory.GetObjects.GetAllPlayers();
            foreach (AmongUsMemory.Player p in playerDatas) p.StartObserveState();
            if (playerDatas.Count == 0) return;

            AmongUsMemory.Player player = playerDatas.Find((p) => p.IsLocalPlayer);

            RunCommand("enablecooldown");
            Program.hackGUI.element_instaKill.Checked = false;
            RunCommand("nohighlight");
            Program.hackGUI.element_highlightImposters.Checked = false;
            RunCommand("nofullbright");
            Program.hackGUI.element_fullbright.Checked = false;

            Console.WriteLine("Reset");
        }

        public static void Update()
        {
            //AmongUsMemory.Player player = playerDatas.Find((p) => p.IsLocalPlayer);
            //if (player == null) return;
            //Console.WriteLine(player.Position.x + "f, " + player.Position.y + "f");
        }

        public static int RunCommand(string input)
        {
            Player player = playerDatas.Find((p) => p.IsLocalPlayer);

            string[] playerCommands = new string[] { "dead", "alive", "imposter", "crewmate", "fullbright", "nofullbright", "highlightimposters", "nohighlight" };
            if (player == null && playerCommands.Contains(input))
            {
                Reset();
                player = playerDatas.Find((p) => p.IsLocalPlayer);
                if (player == null)
                {
                    Console.WriteLine(input + " -> No local player. Do 'reset' or be in a in-progress game");
                    return -1;
                }
            }

            switch (input)
            {
                case "reset":
                    Reset();
                    Console.WriteLine(input + " -> ok");
                    break;
                case "quit":
                case "exit":
                case "stop":
                    Program.Exit();
                    break;

                case "dead":
                    player?.Set_IsDead(Convert.ToByte(true));
                    Console.WriteLine(input + " -> ok");
                    break;
                case "alive":
                    player?.Set_IsDead(Convert.ToByte(false));
                    Console.WriteLine(input + " -> ok");
                    break;
                case "imposter":
                    SetImposter(player, true);
                    Console.WriteLine(input + " -> ok");
                    break;
                case "crewmate":
                    SetImposter(player, false);
                    Console.WriteLine(input + " -> ok");
                    break;
                case "fullbright":
                    IntPtr lightSourcePtr = Utils.GetMemberPointer(player.Instance.myLight, typeof(LightSource), "LightRadius");
                    Main.mem.FreezeValue(lightSourcePtr.GetAddress(), "float", "2000.0");
                    Console.WriteLine(input + " -> ok");
                    break;
                case "nofullbright":
                    IntPtr lightSourcePtr2 = Utils.GetMemberPointer(player.Instance.myLight, typeof(LightSource), "LightRadius");
                    Main.mem.UnfreezeValue(lightSourcePtr2.GetAddress());
                    Console.WriteLine(input + " -> ok");
                    break;
                case "highlightimposters":
                    playerDatas.FindAll((p) => p.PlayerInfo?.IsImpostor == 1).ForEach((p) => p.Set_SetNameTextColor(new Color(255, 0, 0, 1)));
                    Console.WriteLine(input + " -> ok");
                    break;
                case "nohighlight":
                    playerDatas.ForEach((p) => p.Set_SetNameTextColor(new Color(255, 255, 255, 1)));
                    Console.WriteLine(input + " -> ok");
                    break;
                case "disablecooldown":
                    if (!fakeImp)
                    {
                        IntPtr killTimerPtr = AmongUsMemory.Utils.GetMemberPointer(player.PlayerControlOffsetPtr, typeof(PlayerControl), "killTimer");
                        AmongUsMemory.Main.mem.FreezeValue(killTimerPtr.GetAddress(), "float", "0.0");
                    }
                    break;
                case "enablecooldown":
                    if (!fakeImp)
                    {
                        IntPtr killTimerPtr = AmongUsMemory.Utils.GetMemberPointer(player.PlayerControlOffsetPtr, typeof(PlayerControl), "killTimer");
                        AmongUsMemory.Main.mem.UnfreezeValue(killTimerPtr.GetAddress());
                    }
                    break;
                case "test":
                    //AmongUsMemory.GetObjects.GetAmongUsClient();
                    IntPtr targetPointer = Utils.GetMemberPointer(player.Instance.nameText, typeof(TextRenderer), "Color");
                    Console.WriteLine(Main.mem.ReadFloat(targetPointer.GetAddress()));
                    Console.WriteLine((targetPointer + 4).GetAddress());
                    Console.WriteLine((targetPointer + 8).GetAddress());
                    Console.WriteLine((targetPointer + 12).GetAddress());
                    break;
            }
            return 0;
        }

        #region Set
        private static void SetImposter(AmongUsMemory.Player player, bool setIsImposter)
        {
            // keep track if the player is fake impostering to prevent killing while crewmate which doesnt actually register
            IntPtr isImposterPtr = Utils.GetMemberPointer(player.PlayerInfoOffsetPtr, typeof(PlayerInfo), "IsImpostor");
            int isImposter = Main.mem.ReadByte(isImposterPtr.GetAddress());
            if ((fakeImp == false && isImposter == 0) || fakeImp == true) fakeImp = setIsImposter;
            player?.Set_Impostor(Convert.ToByte(setIsImposter));

            IntPtr killTimerPtr = Utils.GetMemberPointer(player.PlayerControlOffsetPtr, typeof(PlayerControl), "killTimer");
            if (fakeImp)
            {
                Program.hackGUI.element_instaKill.Checked = false;
                Program.hackGUI.element_instaKill.Enabled = false;
                Main.mem.FreezeValue(killTimerPtr.GetAddress(), "float", "60.0");
            }
            else
            {
                Program.hackGUI.element_instaKill.Enabled = true;
                Main.mem.UnfreezeValue(killTimerPtr.GetAddress());
            }
        }

        public static int SetKillCoolDown(float seconds)
        {
            if (fakeImp) return 1;
            Player player = playerDatas.Find((p) => p.IsLocalPlayer);
            if (player == null)
            {
                Reset();
                player = playerDatas.Find((p) => p.IsLocalPlayer);
                if (player == null)
                    return -1;
            }
            IntPtr killTimerPtr = Utils.GetMemberPointer(player.PlayerControlOffsetPtr, typeof(PlayerControl), "killTimer");
            Main.mem.FreezeValue(killTimerPtr.GetAddress(), "float", seconds.ToString("0.0"));
            return 0;
        }

        public static int SetBrightness(float brightness)
        {
            Player player = playerDatas.Find((p) => p.IsLocalPlayer);
            if (player == null)
            {
                Reset();
                player = playerDatas.Find((p) => p.IsLocalPlayer);
                if (player == null)
                    return -1;
            }

            IntPtr lightSourcePtr = Utils.GetMemberPointer(player.Instance.myLight, typeof(LightSource), "LightRadius");
            Main.mem.FreezeValue(lightSourcePtr.GetAddress(), "float", brightness.ToString("0.0"));
            return 0;
        }
        #endregion
    }
}
