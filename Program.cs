using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace CS_Triggerbot
{
    class EntityData  //create a prototype for storing entity data later on.
    {
        public float ent_x;
        public float ent_y;
        public float ent_z;

        public int ent_health;
        public int ent_team;
        public int ent_number;

        public EntityData(float x, float y, float z, int health, int team, int entitynumber)
        {
            ent_x = x;
            ent_y = y;
            ent_z = z;
            ent_health = health;
            ent_team = team;
            ent_number = entitynumber;
        }
    }

    class Program
    {
        //Globals
        public static int updatetime = 100;
        public static bool showinfomessages =               true;
        public static bool showdebugmessages =              false;
        public static bool showhealth =                     false;
        public static bool showcalculations =               false;
        public static bool showtriggerbotmessages =         true;
        public static bool enabletriggerbot =               true;

        //Client Addresses
        public static String process = "hl2";
        public static String modulename = "client.dll";
        public static Int32 a_localplayer = 0x4c6708;
        public static Int32 a_entitylist = 0x004D3914;
        public static Int32 o_health = 0x94;
        public static Int32 o_teamnumber = 0x9c;
        public static Int32 o_crosshairentity = 0x14F0;
        public static Int32 o_entitynumber = 0x54;
        public static Int32 o_coordinates = 0x260;
        public static Int32 o_viewangle_pitch = 0x26C;
        public static Int32 o_viewangle_yaw = 0x270;

        //Engine Addresses
        public static Int32 a_playercount = 0x000591F4;
        public static Int32 o_playercount = 0x164;

        //Bot Tolerance Values
        public static int angletolerance = 5; //measurement in degrees on how tight your cursor must be to the enemy

        //Set up the Mouse
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        static void Main(string[] args)
        {
            Program p = new Program();
            VAMemory vam = new VAMemory(process);
            Int32 baseaddress = p.GetModuleBaseAddress(process, modulename);
            Int32 localplayerptr = baseaddress + a_localplayer;
            Int32 localplayeraddr = vam.ReadInt32((IntPtr)localplayerptr);

            baseaddress = p.GetModuleBaseAddress(process, modulename);

            localplayerptr = baseaddress + a_localplayer;
            localplayeraddr = vam.ReadInt32((IntPtr)localplayerptr);
            Int32 healthaddress = localplayeraddr + o_health;

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;


            int health;
            int playercount = p.GetPlayerCount(vam);
             playercount = 61;

            float botx;
            float boty;
            float botz;

            float playerx;
            float playery;
            float playerz;
            float playeryaw;

            List<EntityData> enemylist = new List<EntityData>();

            System.Console.CursorVisible = false;

            while (true)
            {
                if(p.InGame(vam, localplayeraddr) != true)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine("NOT IN GAME");

                    while (p.InGame(vam, localplayeraddr) == false)
                    {
                        baseaddress = p.GetModuleBaseAddress(process, modulename);
                        localplayerptr = baseaddress + a_localplayer;
                        localplayeraddr = vam.ReadInt32((IntPtr)localplayerptr);
                        Thread.Sleep(100);
                    }

                    System.Console.ForegroundColor = ConsoleColor.White;
                    baseaddress = p.GetModuleBaseAddress(process, modulename);
                    localplayerptr = baseaddress + a_localplayer;
                    localplayeraddr = vam.ReadInt32((IntPtr)localplayerptr);
                    Thread.Sleep(100);
                }

                playerx = p.GetPlayerX(vam, localplayeraddr);
                playery = p.GetPlayerY(vam, localplayeraddr);
                playerz = p.GetPlayerZ(vam, localplayeraddr);
                playeryaw = -(p.GetPlayerYaw(vam, localplayeraddr));

                if (showhealth) 
                {
                    health = vam.ReadInt32((IntPtr)healthaddress);

                    //Write Health to the Console
                    System.Console.ForegroundColor = ConsoleColor.Green;

                    if (health <= 1)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine("Health: DEAD");
                    }
                    else
                    {
                        System.Console.WriteLine("Health: " + health);
                    }

                    System.Console.ForegroundColor = ConsoleColor.White;
                }

                //find yaw angle using trigonometric functions and x y coordinates (overhead cartesian)
                enemylist = p.BuildEnemyList(playercount, p.GetEnemyTeamNumber(vam, localplayeraddr), vam, baseaddress); //COUNTER-TERRORISTS ARE 3, TERRORISTS 2 
                for (int i = 0; i < (enemylist.Count); i++)
                {
                    p.TriggerLogic(playerx, playery, playeryaw, enemylist[i], vam, localplayeraddr);
                }

                if (showinfomessages)
                {
                    System.Console.WriteLine("");
                    System.Console.WriteLine("Base Address: 0x" + localplayeraddr.ToString("X"));
                    System.Console.WriteLine("Local Player Object Address: 0x" + localplayeraddr.ToString("X"));
                    System.Console.WriteLine("Health Address: 0x" + healthaddress.ToString("X"));
                    System.Console.WriteLine("Crosshair Entity: " + p.GetCrosshairEntity(vam, localplayeraddr));
                    System.Console.WriteLine("Player X: " + p.GetPlayerX(vam, localplayeraddr));
                    System.Console.WriteLine("Player Y: " + p.GetPlayerY(vam, localplayeraddr));
                    System.Console.WriteLine("Player Z: " + p.GetPlayerZ(vam, localplayeraddr));
                    System.Console.WriteLine("Player Pitch: " + (p.GetPlayerPitch(vam, localplayeraddr)));
                    System.Console.WriteLine("Player Yaw: " + (p.GetPlayerYaw(vam, localplayeraddr)));
                    System.Console.WriteLine("Player Count: " + playercount);
                    System.Console.WriteLine("Number of Registered Enemies: " + enemylist.Count);
                }

                enemylist = null;

                Thread.Sleep(updatetime);
                System.Console.Clear();
            } 
        }
        
        public bool InGame(VAMemory vam, Int32 localplayeraddr)
        {
                if (vam.ReadInt32((IntPtr)GetModuleBaseAddress(process, modulename) + a_localplayer) != 0x00)
                {
                    return true;
                }
                return false;
        }

        public List<EntityData> BuildEnemyList(int playercount, int enemyteamnumber, VAMemory vam, Int32 baseaddress) //returns an list of floats arranged in (x, y, z) * number of players
        {
            Int32 a_playerobject;

            float entityx;
            float entityy;
            float entityz;
            int entityhealth;
            int team;
            int entitynumber;

            List<EntityData> retlist = new List<EntityData>();

            //iterate through the entitylist for the players and skipping local player
            for (int i = 0; i < playercount; i++) 
            {
                //read the player object address
                a_playerobject = vam.ReadInt32((IntPtr) baseaddress + a_entitylist + (0x10 * (i)));  

                entityx = vam.ReadFloat((IntPtr)a_playerobject + o_coordinates);
                entityy = vam.ReadFloat((IntPtr)a_playerobject + o_coordinates + 0x4);
                entityz = vam.ReadFloat((IntPtr)a_playerobject + o_coordinates + 0x8);
                entityhealth = vam.ReadInt32((IntPtr)a_playerobject + o_health);
                team = vam.ReadInt32((IntPtr) a_playerobject + o_teamnumber);
                entitynumber = vam.ReadInt32((IntPtr)a_playerobject + o_entitynumber);

                //add to the return list
                
                if (team == enemyteamnumber)
                {
                    retlist.Add((new EntityData(entityx, entityy, entityz, entityhealth, team, entitynumber)));
                }


                if (showdebugmessages)
                {
                    System.Console.WriteLine("Entity Pointer: 0x" + (baseaddress + a_entitylist + (0x10 * (i))).ToString("X"));
                    System.Console.WriteLine("Entity Object Address: 0x" + a_playerobject.ToString("X"));
                    System.Console.WriteLine("Entity X: " + entityx);
                    System.Console.WriteLine("Entity Y: " + entityy);
                    System.Console.WriteLine("Entity Z: " + entityz);
                }

            }
            return retlist;
        }

        public int GetPlayerCount(VAMemory vam)
        {
            Int32 enginebaseaddress = GetModuleBaseAddress(process, "engine.dll");
            Int32 playercount = vam.ReadInt32((IntPtr)enginebaseaddress + a_playercount);
            playercount = vam.ReadInt32((IntPtr)playercount + o_playercount);
            return playercount;
        } //reading this too often seems to break shit

        public void TriggerLogic(float playerx, float playery, float playeryaw, EntityData e, VAMemory vam, Int32 localplayeraddr)
        {
            bool ontarget = false;
            float xdistance = playerx - e.ent_x;
            float ydistance = playery - e.ent_y;
            float calculatedangle = (float)(180 * (Math.Atan2(ydistance, xdistance) / Math.PI));
            float absoluteangle = Math.Abs(calculatedangle + playeryaw);

            if(showcalculations)
            {
                System.Console.WriteLine("X Distance: " + xdistance);
                System.Console.WriteLine("Y Distance: " + ydistance);
                System.Console.WriteLine("Calculated Angle: " + calculatedangle);
                System.Console.WriteLine("Absolute Angle: " + absoluteangle);
            }
            if (e.ent_health != 1 && e.ent_health != 0) { //only do this if the entity is alive. else print dead


                if (absoluteangle > 175 && absoluteangle < 185 && GetCrosshairEntity(vam, localplayeraddr) == e.ent_number)
                {
                    if (showtriggerbotmessages)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Green;
                        System.Console.WriteLine("ON TARGET");
                        System.Console.ForegroundColor = ConsoleColor.White;
                        ontarget = true;
                    }
                }
                else if (absoluteangle > 180 - angletolerance && absoluteangle < 180 + angletolerance && GetCrosshairEntity(vam, localplayeraddr) != e.ent_number)
                {
                    if (showtriggerbotmessages)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Blue;
                        System.Console.WriteLine("TARGET OBSTRUCTED");
                        System.Console.ForegroundColor = ConsoleColor.White;
                        ontarget = false;
                    }
                }
                else
                {
                    if (showtriggerbotmessages)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine("OFF TARGET");
                        System.Console.ForegroundColor = ConsoleColor.White;
                        ontarget = false;
                    }
                }

                if (ontarget && GetCrosshairEntity(vam, localplayeraddr) == e.ent_number && enabletriggerbot)
                {
                    Point point = Cursor.Position;
                    Int32 entityid = GetCrosshairEntity(vam, localplayeraddr);
                    for (int i = 0; i < 5; i++)  //spam the fuck out of the mouse
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)point.X, (uint)point.Y, 0, 0);

                        Thread.Sleep(50);
                    }
                }
            }
            else
            {
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine("DEAD or ENTITY UNLOADED");
                System.Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public Int32 GetCrosshairEntity(VAMemory vam, int localplayeraddr)
        {
            Int32 crosshairentity = vam.ReadInt32((IntPtr) localplayeraddr + o_crosshairentity);

            return crosshairentity;
        } //gets the entity within the player's crosshair

        public Int32 GetPlayerTeamNumber(VAMemory vam, int localplayeraddr)
        {
            return vam.ReadInt32((IntPtr)localplayeraddr + o_teamnumber);
        }//gets the player's team number

        public Int32 GetEnemyTeamNumber(VAMemory vam, int localplayeraddr)
        {
            Int32 x = GetPlayerTeamNumber(vam, localplayeraddr);
            if (x == 3) return 2; //if you're a counter terrorist, return terrorist
            if (x == 2) return 3; //if you're a terrorist, return counter terrorist
            if (x == 1) return 1; //if spectator return spectator
            return -1;
        }

        public float GetPlayerX(VAMemory vam, int localplayeraddr)  //front to back
        {
            float x = vam.ReadFloat((IntPtr) localplayeraddr + o_coordinates);

            return x;

        }

        public float GetPlayerY(VAMemory vam, int localplayeraddr)  //side to side
        {
            float y = vam.ReadFloat((IntPtr)  localplayeraddr + o_coordinates + 4);

            return y;
        }

        public float GetPlayerZ(VAMemory vam, int localplayeraddr)  //up and down
        {
            float z = vam.ReadFloat((IntPtr) localplayeraddr + o_coordinates + 8);

            return z;
        }

        public float GetPlayerPitch(VAMemory vam, int localplayeraddr)  //up and down
        {
            float pitch = vam.ReadFloat((IntPtr)localplayeraddr + o_viewangle_pitch);

            return pitch;
        }

        public float GetPlayerYaw(VAMemory vam, int localplayeraddr)  //up and down
        {
            float yaw = vam.ReadFloat((IntPtr)localplayeraddr + o_viewangle_yaw);

            return yaw;
        }

        public Int32 GetModuleBaseAddress(String process, String modulename)
        {
            try
            {
                Process[] p = Process.GetProcessesByName(process);
                if (p.Length > 0)
                {
                    foreach (ProcessModule m in p[0].Modules)
                    {
                        if (m.ModuleName == modulename)
                        {
                            return (Int32)m.BaseAddress;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace);
                Console.Write("Base Address Could Not Be Found.");
                System.Environment.Exit(1);
                
            }
            return 0;
      
        } //resolve the process module's base address

    }

}


