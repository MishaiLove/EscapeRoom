using System;

namespace EscapeRoom
{
    internal static class Program
    {
        private static void Main()
        {
            Console.CursorVisible = false;

            var game = new EscapeRoomGame();
            game.Run();

            Console.CursorVisible = true;
            Console.ResetColor();
        }
    }

    /// Console escape room game: collect the key, open the door, and leave the room through the open door.
    internal sealed class EscapeRoomGame
    {
        private const char WallSymbol = '#';
        private const char FloorSymbol = '.';
        private const char PlayerSymbol = '!';
        private const char KeySymbol = '?';
        private const char DoorClosedSymbol = ';';
        private const char DoorOpenSymbol = ':';

        private static readonly Random _random = new();

        private char[,] _map = default!;

        private int _width;
        private int _height;

        private int _playerX;
        private int _playerY;

        private int _keyX;
        private int _keyY;

        private int _doorX;
        private int _doorY;

        private bool _hasKey;
        private bool _isDoorOpen;

        /// Starts the game and runs the main loop.
        public void Run()
        {
            ShowInstructions();
            ReadRoomSize(out _width, out _height);

            CreateRoom();
            PlaceDoor();
            PlacePlayerAndKey();

            Console.Clear();
            DrawEntireMap();
            DrawStatusLine();

            RunInputLoop();
        }

        private void ShowInstructions()
        {
            Console.Clear();
            Console.WriteLine("Escape Room\n");
            Console.WriteLine("Goal: collect the key, open the door, and leave the room.\n");
            Console.WriteLine("Controls:");
            Console.WriteLine("  Arrow keys -> move (one step per key press)");
            Console.WriteLine("  ESC        -> quit\n");
            Console.WriteLine("Legend:");
            Console.WriteLine($"  {PlayerSymbol} = player");
            Console.WriteLine($"  {KeySymbol} = key");
            Console.WriteLine($"  {DoorClosedSymbol} = closed door");
            Console.WriteLine($"  {DoorOpenSymbol} = open door");
            Console.WriteLine($"  {WallSymbol} = wall");
            Console.WriteLine($"  {FloorSymbol} = floor\n");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        private void ReadRoomSize(out int width, out int height)
        {
            const int minWidth = 10;
            const int minHeight = 6;
            const int maxWidth = 120;
            const int maxHeight = 40;

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Enter room size (width {minWidth}-{maxWidth}, height {minHeight}-{maxHeight})\n");

                width = ReadInteger("Width: ");
                height = ReadInteger("Height: ");

                if (width < minWidth || width > maxWidth || height < minHeight || height > maxHeight)
                {
                    Console.WriteLine("\nInvalid input: out of allowed range.");
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey(true);
                    continue;
                }

                if (width - 2 < 2 || height - 2 < 2)
                {
                    Console.WriteLine("\nInvalid input: interior area is too small.");
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey(true);
                    continue;
                }

                return;
            }
        }

        private int ReadInteger(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int value))
                {
                    return value;
                }

                Console.WriteLine("Please enter a valid integer.");
            }
        }

        private void CreateRoom()
        {
            _map = new char[_height, _width];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == _width - 1 || y == _height - 1;
                    _map[y, x] = isBorder ? WallSymbol : FloorSymbol;
                }
            }
        }

        private void PlaceDoor()
        {
            int sideIndex = _random.Next(4); // 0=top, 1=bottom, 2=left, 3=right

            if (sideIndex == 0)
            {
                _doorY = 0;
                _doorX = _random.Next(1, _width - 1);
            }
            else if (sideIndex == 1)
            {
                _doorY = _height - 1;
                _doorX = _random.Next(1, _width - 1);
            }
            else if (sideIndex == 2)
            {
                _doorX = 0;
                _doorY = _random.Next(1, _height - 1);
            }
            else
            {
                _doorX = _width - 1;
                _doorY = _random.Next(1, _height - 1);
            }

            _isDoorOpen = false;
            _map[_doorY, _doorX] = DoorClosedSymbol;
        }

        private void PlacePlayerAndKey()
        {
            (_playerX, _playerY) = GetRandomInteriorPosition();
            _map[_playerY, _playerX] = PlayerSymbol;

            do
            {
                (_keyX, _keyY) = GetRandomInteriorPosition();
            } while (_keyX == _playerX && _keyY == _playerY);

            _map[_keyY, _keyX] = KeySymbol;

            _hasKey = false;
        }

        private (int x, int y) GetRandomInteriorPosition()
        {
            int x = _random.Next(1, _width - 1);
            int y = _random.Next(1, _height - 1);
            return (x, y);
        }

        private void RunInputLoop()
        {
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    return;
                }

                if (TryGetMoveDelta(keyInfo.Key, out int dx, out int dy) == false)
                {
                    continue;
                }

                bool didWin = TryMovePlayer(dx, dy);
                if (didWin)
                {
                    ShowWinMessage();
                    return;
                }
            }
        }

        private bool TryGetMoveDelta(ConsoleKey key, out int dx, out int dy)
        {
            dx = 0;
            dy = 0;

            switch (key)
            {
                case ConsoleKey.LeftArrow:
                    dx = -1;
                    return true;

                case ConsoleKey.RightArrow:
                    dx = 1;
                    return true;

                case ConsoleKey.UpArrow:
                    dy = -1;
                    return true;

                case ConsoleKey.DownArrow:
                    dy = 1;
                    return true;

                default:
                    return false;
            }

        }

        private bool TryMovePlayer(int dx, int dy)
        {
            int targetX = _playerX + dx;
            int targetY = _playerY + dy;

            if (IsOutsideRoom(targetX, targetY))
            {
                return _isDoorOpen && _playerX == _doorX && _playerY == _doorY;   
            }

            char targetCell = _map[targetY, targetX];

            if (targetCell == WallSymbol || targetCell == DoorClosedSymbol)
            {
                return false;
            }


            if (targetCell == KeySymbol)
            {
                _hasKey = true;
                TryOpenDoor();
            }

            UpdatePlayerPosition(targetX, targetY);
            DrawStatusLine();

            return false;
        }

        private void TryOpenDoor()
        {
            if (_hasKey == false || _isDoorOpen)
            {
                return;
            }


            _isDoorOpen = true;
            _map[_doorY, _doorX] = DoorOpenSymbol;
            DrawCell(_doorX, _doorY, DoorOpenSymbol);
        }

        private void UpdatePlayerPosition(int targetX, int targetY)
        {
            char oldCell = (_isDoorOpen && _playerX == _doorX && _playerY == _doorY) ? DoorOpenSymbol : FloorSymbol;
            _map[_playerY, _playerX] = oldCell;
            DrawCell(_playerX, _playerY, oldCell);

            _playerX = targetX;
            _playerY = targetY;

            _map[_playerY, _playerX] = PlayerSymbol;
            DrawCell(_playerX, _playerY, PlayerSymbol);
        }

        private bool IsOutsideRoom(int x, int y)
        {
            return x < 0 || y < 0 || x >= _width || y >= _height;
        }

        private void DrawEntireMap()
        {
            Console.SetCursorPosition(0, 0);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Console.Write(_map[y, x]);
                }

                Console.WriteLine();
            }
        }

        private void DrawCell(int x, int y, char symbol)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(symbol);
        }

        private void DrawStatusLine()
        {
            int statusY = Math.Min(_height + 1, Console.BufferHeight - 1);

            Console.SetCursorPosition(0, statusY);

            int clearLength = Math.Min(Console.WindowWidth - 1, Math.Max(0, _width + 40));
            Console.Write(new string(' ', clearLength));

            Console.SetCursorPosition(0, statusY);
            Console.Write($"Key = {(_hasKey ? "YES" : "NO")} | Door = {(_isDoorOpen ? "OPEN" : "CLOSED")} | ESC to quit");
        }

        private void ShowWinMessage()
        {
            int messageY = Math.Min(_height + 3, Console.BufferHeight - 1);
            Console.SetCursorPosition(0, messageY);

            Console.WriteLine("Congratulations. You escaped.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
