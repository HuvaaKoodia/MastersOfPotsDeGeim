﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.IO;

namespace MastersOfPotsDeGeimWorld
{
    /// <summary>
    /// Has both the map and all game entities for convenience.
    /// </summary>
    public class Map
    {
        public bool DisableVictoryConditions = false;

        public Tile[,] map_tiles { get; private set; }
        public List<Entity> GameEntities { get; private set; }
        public List<Team> Teams { get; private set; }

        int diamond_amount = 0;

        public int W { get { return map_tiles.GetLength(0); } }
        public int H { get { return map_tiles.GetLength(1); } }
        
        public Map( int w,int h){
            map_tiles = new Tile[w, h];
            GameEntities = new List<Entity>();
            Teams=new List<Team>();
        }

        public void GenerateMap(int seed,int diamonds, int foods,int walls) {
            
            diamond_amount = diamonds;

            var rand = new Random(seed);

            for (int i = 0; i < W; ++i)
            {
                for (int j = 0; j < H; ++j)
                {
                    map_tiles[i, j] = new Tile(i,j);
                }
            }

            int fail_safe = 10000;
            int rx,ry;
            while(diamonds>0){
                --fail_safe;
                if (fail_safe == 0) break;

                rx=rand.Next(W);
                ry=rand.Next(H);

                var t = GetTile(rx, ry);

                if (t.IsEmpty())
                {
                    t.TileType = Tile.Type.diamond;
                    t.Amount = 1+rand.Next(14);
                }
                else continue;

                --diamonds;
            }
            fail_safe = 10000;

            while (foods > 0)
            {
                --fail_safe;
                if (fail_safe == 0) break;

                rx = rand.Next(W);
                ry = rand.Next(H);

                var t = GetTile(rx, ry);

                if (t.IsEmpty())
                {
                    t.TileType = Tile.Type.food;
                    t.Amount = 5 + rand.Next(25);
                }
                else continue;

                --foods;
            }
            fail_safe = 10000;

            while (walls > 0)
            {
                --fail_safe;
                if (fail_safe == 0) break;

                rx = rand.Next(W);
                ry = rand.Next(H);

                var t = GetTile(rx, ry);

                if (ry == 0 || ry == H - 1 || ry % 3 == 0) continue;

                if (t.IsEmpty())
                {
                    t.TileType = Tile.Type.wall;
                }
                else continue;

                --walls;  
            }
        }

        public void SetTile(int x, int y, Tile.Type t)
        {
            var tile = map_tiles[x, y];
            tile.TileType = t;
        }

        public void SetTile(int x, int y, int a)
        {
            var tile = map_tiles[x, y];
            tile.Amount = a;
        }

        public void SetTile(int x, int y, Entity e)
        {
            var tile = map_tiles[x, y];
            tile.EntityReference=e;
        }

        private Tile wall_tile = new Tile(Tile.Type.wall);

        public Tile GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x > W - 1 || y > H - 1) return wall_tile;
            return map_tiles[x, y];
        }

        void ResetGameState() {
            GameEntities.Clear();

            foreach (var team in Teams) {
                team.ResetStats();
            }
        }

        //logic

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            waiting_for_timer = false;
        }

        Timer timer = new Timer();
        bool waiting_for_timer = false;
        public int Turn { get; private set; }

        public void GameLoop() {

            bool master_loop=true;
            while (master_loop)
            {
                Console.Clear();
                WriteLineRandomColors("Masters of Bots : Game of Diamonds. \nV.1.0");

                Console.WriteLine("\nCompeting teams: ");
                foreach (var t in Teams)
                {
                    Console.WriteLine("- Team " + t.Name);
                }
                Console.WriteLine();
                //pre simulation set up
                var r = new Random();
                int seed = r.Next();

                while (true)
                {
                    Console.WriteLine("Current seed: " + seed);

                    Console.WriteLine("Input:\n- \"c\" to change seed\n- \"r\" to randomize seed\n\n- \"s\" to start\n\n- \"e\" to exit");
                    var input = Console.ReadLine();
                    if (input == "c")
                    {
                        Console.WriteLine("Input new seed.");
                        var inp = Console.ReadLine();
                        seed = inp.GetHashCode();
                    }
                    else if (input == "r")
                    {
                        Console.WriteLine("Randomizing seed.");
                        seed = r.Next();
                    }
                    else if (input == "s")
                    {
                        break;
                    }
                    else if (input.StartsWith("e")){
                        master_loop = false;
                        break;
                    }
                }
                if (!master_loop) break;

                Console.Clear();
                Console.WriteLine("Simulation Start\n\n");
                GenerateMap(seed, 10, 10, 20);
                ResetGameState();

                //DEV.temp hax team starting positions
                Teams[0].AddUnitToMap(2, 2);
                Teams[1].AddUnitToMap(W - 3, H - 3);

                //simulation
                Turn = 0;
                bool gameOn = true, allowInput = true, draw_map_full = false, draw_map = true;
                int autoRun = 0;
                DrawMap();

                timer.Interval = 250;
                timer.Elapsed += TimerElapsed;

                var s_out = Console.Out;
                StringWriter s_wrt = new StringWriter();


                Team winner = null;
                bool show_gameover = true;

                while (gameOn)
                {
                    for (int e = GameEntities.Count - 1; e >= 0; --e)
                    {
                        //draw mode logic
                        draw_map = true;
                        allowInput = true;

                        if (draw_map_full && e != 0)
                        {
                            allowInput = false;
                            draw_map = false;
                        }

                        if (waiting_for_timer)
                        {
                            ++e;
                            continue;
                        }
                        timer.Stop();

                        ++Turn;
                        var entity = GameEntities[e];

                        //auto run logic
                        if (autoRun != 0)
                        {
                            allowInput = false;

                            if (autoRun > 0) --autoRun;
                            if (autoRun == 0)
                            {
                                allowInput = true;
                            }
                        }

                        //input
                        if (allowInput)
                        {
                            Console.SetOut(s_out);
                            Console.WriteLine("Input:\n- \"exit\" to end program\n- \"auto n\" to autorun simulation.\n(n = amount of turns, no parameter runs the rest of the simulation)\n- \"draw a\" to draw every entity turn\n- \"draw f\" to draw every full team turn\n\n- anykey to continue simulation");

                            while (true)
                            {
                                var input = Console.ReadLine();

                                if (input == ("exit"))
                                {
                                    gameOn = false;
                                    show_gameover = false;
                                    break;
                                }
                                else if (input.StartsWith("auto"))
                                {
                                    try
                                    {
                                        if (input.Length > 4)
                                        {
                                            int steps = int.Parse(input.Substring(5));
                                            autoRun = steps;
                                        }
                                        else
                                        {
                                            autoRun = -1;
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Faulty syntax. Try: \"auto 100\"");
                                        continue;
                                    }
                                }
                                else if (input.StartsWith("draw"))
                                {
                                    try
                                    {
                                        var command = input.Substring(5, 1);
                                        if (command == "a")
                                        {
                                            draw_map_full = false;
                                            Console.WriteLine("Drawing every turn.");
                                        }
                                        else if (command == "f")
                                        {
                                            draw_map_full = true;
                                            Console.WriteLine("Drawing every full turn only.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Command " + command + " not supported.");
                                        }
                                        continue;
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Faulty syntax. Try: \"draw a\"");
                                        continue;
                                    }
                                }
                                else
                                {
#if DEBUG
                                    entity.GetInput(input);
#endif
                                }
                                break;
                            }

                            if (!gameOn) break;
                        }


                        if (draw_map)
                        {
                            timer.Start();
                            waiting_for_timer = true;

                            Console.Clear();

                            s_wrt.GetStringBuilder().Clear();
                        }
                        Console.SetOut(s_wrt);

                        //updates
                        entity.MyTeam.Update(entity);
                        entity.Update();
                        entity.LateUpdate();

                        if (entity.Dead) GameEntities.Remove(entity);

                        //drawing
                        if (draw_map)
                        {
                            Console.SetOut(s_out);

                            Console.WriteLine("Turn " + Turn);
                            Console.WriteLine(entity.MyTeam);
                            Console.WriteLine(entity);

                            DrawMap();

                            if (!draw_map_full)
                            {
                                Console.Write(s_wrt.ToString() + "\n");
                            }
                        }

                        if (DisableVictoryConditions) continue;
                        //game over (lazy polling checks)
                        if (GameEntities.Count == 0)
                        {
                            gameOn = false;
                            winner = null;
                            break;
                        }
                        //no enemies
                        bool no_teams = true;
                        Team team = entity.MyTeam;
                        winner = team;
                        foreach (var ent in GameEntities)
                        {

                            if (ent.MyTeam != team)
                            {
                                no_teams = false;
                                break;
                            }
                        }

                        if (no_teams)
                        {
                            gameOn = false;
                            break;
                        }

                        //no diamonds
                        bool no_diamonds = true;
                        foreach (var t in map_tiles)
                        {
                            if (t.IsType(Tile.Type.diamond))
                            {
                                no_diamonds = false;
                                break;
                            }
                        }

                        if (no_diamonds)
                        {
                            int max = 0;
                            foreach (var t in Teams)
                            {
                                if (t.DiamondCount > max)
                                {
                                    winner = t;
                                    max = t.DiamondCount;
                                }
                            }
                            gameOn = false;
                            break;
                        }
                    }

                }
                

                Console.Clear();
                Console.SetOut(s_out);
                if (!show_gameover) continue;
                DrawMap();

                //gameover report
                Console.WriteLine("Gameover!");

                if (winner == null)
                {
                    Console.WriteLine("It's a tie!!!");
                }
                else
                {
                    Console.WriteLine("\nWinner:");
                    Console.WriteLine("- " + winner);

                    Console.WriteLine("\nLosers:");
                    foreach (var t in Teams)
                    {
                        if (t == winner) continue;
                        Console.WriteLine("- " + t);
                    }

                    Console.WriteLine("\nPress anykey to continue...");
                    Console.ReadLine();
                }
            }
            
        }


        public void DrawMap()
        {
            for (int j = 0; j < H; ++j)
            {
                for (int i = 0; i < W; ++i)
                {
                    var t = GetTile(i, j);

                    Console.ForegroundColor = t.GetCharacterColor();
                    Console.Write(t.GetCharacterCode() + "");
                    Console.Write(" ");
                }
                Console.WriteLine();
            }

            Console.ResetColor();
        }

        ConsoleColor[] colors = { ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.Cyan, ConsoleColor.DarkYellow, ConsoleColor.Blue,ConsoleColor.Magenta};

        private void WriteLineRandomColors(string text){
            int ci = 0;
            var spl = text.Split(' ');

            foreach (var s in spl) {
                Console.ForegroundColor = colors[ci];
                Console.Write(s+" ");
                ++ci;
                if (ci == colors.Length) ci = 0;
            }
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
