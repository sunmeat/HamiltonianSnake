// inspired by https://www.youtube.com/watch?v=IDjFu7uxkVI&ab_channel=CodeWizer

using System.Text;

class HamiltonianCycle // https://github.com/Goldenbarky/HamiltonianCycleGenerator/blob/master/HamiltonianCycle.cs
{
    public enum Direction { Left, Down, Right, Up }

    public static int[,]? grid;

    public static void Create(int width, int height)
    {
        grid = CreateHamiltonianGuide(height - 2, width - 2);

        if (FollowGuide(0, 0, 1, Direction.Right, grid) is var values)
        {
            if (!values.Item1)
            {
                Console.WriteLine("HamiltonianCycle.Create FAIL");
            }
        }

        grid = CleanUpGrid(grid);

        Console.WriteLine("Hamiltonian cycle found!");
        Thread.Sleep(1000);
        Console.Clear();
    }

    // returns the unit vector to travel in the given direction
    public static (int, int) ConvertDirection(Direction dir) => dir switch
    {
        Direction.Right => (0, 1),
        Direction.Down => (1, 0),
        Direction.Left => (0, -1),
        Direction.Up => (-1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
    };

    // returns the given direction's "complement", i.e. the next iterative direction to search for the wall
    // used to keep the program's "right hand" on the wall at all times
    public static Direction ComplementDirection(Direction dir) => dir switch
    {
        Direction.Right => Direction.Down,
        Direction.Down => Direction.Left,
        Direction.Left => Direction.Up,
        Direction.Up => Direction.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
    };

    // returns the inverse of the passed direction
    public static Direction InvertDirection(Direction dir) => dir switch
    {
        Direction.Right => Direction.Left,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Up => Direction.Down,
        _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
    };

    // create the pseudo maze for the pathfinding algorithm to solve
    public static int[,] CreateHamiltonianGuide(int baseX, int baseY)
    {
        int[,] grid = new int[baseX * 2 - 1, baseY * 2 - 1];
        int[,] walls = new int[baseX - 1, baseY - 1];

        // Console.WriteLine("Populating edges...");
        PopulateEdges(walls);
        // Console.WriteLine("Generating spanning tree...");
        MinimallySpanningTree(walls);

        Console.WriteLine("Finalizing Guide...");
        TranslateWallsToGrid(grid, walls);

        return grid;
    }

    // generates randomly weighted edges between each node in the grid
    public static void PopulateEdges(int[,] grid)
    {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        var random = new Random();

        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (x % 2 == 1 && y % 2 == 1) grid[x, y] = -1;
                else if (x % 2 == 1 ^ y % 2 == 1) grid[x, y] = random.Next(1, 500);
            }
        }
    }

    // remove edges to create a minimally spanning tree
    public static void MinimallySpanningTree(int[,] grid)
    {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        for (int x = 0; x < length; x += 2)
        {
            for (int y = 0; y < width; y += 2)
            {
                if (x == length - 1 && y == width - 1)
                    break;

                int i = 0;
                int j = 0;

                if (x + 1 < length && (y + 1 >= width || grid[x + 1, y] > grid[x, y + 1]))
                    i = 1;
                else
                    j = 1;

                int prevValue = grid[x + i, y + j];
                grid[x + i, y + j] = -1;

                bool successful = false;

                if (y + j % 2 == 0)
                {
                    PriorityQueue<(int, int), int> nodes = new PriorityQueue<(int, int), int>();

                    if (x + i + 1 < length)
                    {
                        nodes.Enqueue((x + i + 1, y + j), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }

                    nodes.Clear();

                    if (successful && x + i - 1 >= 0)
                    {
                        nodes.Enqueue((x + i - 1, y + j), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }
                }
                else
                {
                    PriorityQueue<(int, int), int> nodes = new PriorityQueue<(int, int), int>();

                    if (y + j + 1 < width)
                    {
                        nodes.Enqueue((x + i, y + j + 1), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }

                    nodes.Clear();

                    if (successful && y + j - 1 >= 0)
                    {
                        nodes.Enqueue((x + i, y + j - 1), 0);
                        successful = CheckCanReachHome(grid, nodes);
                    }
                }

                if (!successful)
                {
                    grid[x + i, y + j] = prevValue;
                }
            }
        }
    }

    // check if still connected to 0,0 with dijkstra's algorithm
    public static bool CheckCanReachHome(int[,] grid, PriorityQueue<(int, int), int> nodes)
    {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);
        var visited = new HashSet<(int, int)>();

        while (nodes.Count > 0)
        {
            var (x, y) = nodes.Dequeue();

            // return true if home
            if (x == 0 && y == 0) return true;

            // iterate over the 4 cardinal directions
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j += 2)
                {
                    if (i != 0) j = 0;

                    // if in bounds and not -1 enqeue the node with the priority of its distance from 0,0
                    if (x + i >= 0 && x + i < length && y + j >= 0 && y + j < width && grid[x + i, y + j] != -1 && !visited.Contains((x + i, y + j)))
                    {
                        nodes.Enqueue((x + i, y + j), (x + i) + (y + j));
                        visited.Add((x + i, y + j));
                    }
                }
            }
        }

        // if no remaining nodes to check node is severed
        return false;
    }

    // move the minmally spanning tree edges to walls in the grid
    public static void TranslateWallsToGrid(int[,] grid, int[,] walls)
    {
        int lenWalls = walls.GetLength(0);
        int widthWalls = walls.GetLength(1);

        int lenGrid = grid.GetLength(0);
        int widthGrid = grid.GetLength(1);

        // populate grid with 0 for final nodes, -1 for walls between nodes and -2 for useless spaces
        for (int x = 0; x < lenGrid; x++)
        {
            for (int y = 0; y < widthGrid; y++)
            {
                grid[x, y] = (x % 2, y % 2) switch
                {
                    (0, 0) => 0,
                    (1, 1) => -1,
                    _ => -2,
                };
            }
        }

        // turn each wall in the tree into it's 2 grid counterparts
        for (int x = 0; x < lenWalls; x++)
        {
            for (int y = 0; y < widthWalls; y++)
            {
                switch ((x % 2, y % 2))
                {
                    case (1, 1):
                        continue;
                    case (0, 0):
                        grid[x * 2 + 1, y * 2 + 1] = -1;
                        continue;
                }

                if (walls[x, y] == -1) continue;

                if (x % 2 == 0)
                {
                    grid[x * 2 + 1, y * 2] = -1;
                    grid[x * 2 + 1, y * 2 + 2] = -1;
                }
                else
                {
                    grid[x * 2, y * 2 + 1] = -1;
                    grid[x * 2 + 2, y * 2 + 1] = -1;
                }
            }
        }

        // changed useless -1 walls to -2 to represent useless spaces
        for (int x = 0; x < lenGrid; x++)
        {
            for (int y = 0; y < widthGrid; y++)
            {
                if (x != 0 && x != lenGrid - 1 && y != 0 && y != widthGrid - 1 && grid[x, y] != 0 && grid[x, y - 1] == -2 && grid[x, y + 1] == -2 && grid[x + 1, y] == -2 && grid[x - 1, y] == -2)
                    grid[x, y] = -2;
            }
        }
    }

    public static (bool, (int, int)[]) FollowGuide(int x, int y, int currNum, Direction dir, int[,] grid)
    {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        (int, int)[] orderVals = new (int, int)[length * width];

        while (currNum <= ((length + 1) / 2) * ((width + 1) / 2))
        {
            // locate direction the wall should be in
            var (i, j) = ConvertDirection(ComplementDirection(dir));

            // base case
            // if wall is not to our right, we're lost
            bool lost;
            if (x + i >= length || x + i < 0 || y + j >= width || y + j < 0 || grid[x, y] == -1)
            {
                (i, j) = ConvertDirection(dir);
                x -= i;
                y -= j;
                lost = true;
            }
            // if out of bounds or hit wall, we're lost
            else if (!((x == 0 || x == length - 1) && (y == 0 || y == width - 1)) && grid[x + i, y + j] != -1)
                lost = true;
            else
                lost = false;

            // if current tile is a node number it
            if (grid[x, y] == 0)
            {
                orderVals[currNum - 1] = (x, y);
                grid[x, y] = currNum++;
            }

            // if lost choose new direction
            if (lost)
            {
                // if hit a wall, invert wall direction for new dir
                // Otherwise move in the direction the wall previously was
                dir = (x + i < length && x + i >= 0 && y + j < width && y + j >= 0 && grid[x + i, y + j] == -1) ? InvertDirection(ComplementDirection(dir)) : ComplementDirection(dir);
            }

            (i, j) = ConvertDirection(dir);
            x += i;
            y += j;
        }

        return (true, orderVals);
    }

    public static int[,] CleanUpGrid(int[,] grid)
    {
        int length = grid.GetLength(0);
        int width = grid.GetLength(1);

        int newLength = (length + 1) / 2;
        int newWidth = (width + 1) / 2;

        int[,] newGrid = new int[newLength, newWidth];

        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (x % 2 == 0 && y % 2 == 0)
                {
                    newGrid[x / 2, y / 2] = grid[x, y];
                }
            }
        }

        return newGrid;
    }
}

class HamiltonSnake
{
    readonly static Random r = new Random();
    static int pause = 15;

    static char wall = Convert.ToChar(0x2593); // https://ru.wikipedia.org/wiki/CP866#CP866
    static char snake = '*';
    static char apple = 'o';
    static char head = (char)1;

    static int width = 50;
    static int height = 30;

    public static int[,] Pad(int[,] input)
    {
        int h = input.GetLength(0);
        int w = input.GetLength(1);
        var output = new int[h + 2, w + 2];

        for (int r = 0; r < h; ++r)
        {
            Array.Copy(input, r * w, output, (r + 1) * (w + 2) + 1, w);
        }

        return output;
    }

    public static void Options()
    {
        Console.Title = "hamiltonian snake";
        Console.CursorVisible = false;

        Console.WriteLine("Please, enter field width (10-100): ");
        width = Convert.ToInt32(Console.ReadLine());
        if (width % 2 == 1) width++;
        Console.WriteLine("Please, enter field height (10-30): ");
        height = Convert.ToInt32(Console.ReadLine());
        if (height % 2 == 1) height++;
        Console.Clear();
    }

    public static void PutChar(int x, int y, ConsoleColor color, char symbol)
    {
        Console.SetCursorPosition(x, y);
        Console.ForegroundColor = color;
        Console.Write(symbol);
    }

    public static void SimpleSnake()
    {
        int max_length = (width - 2) * (height - 2); // max length of snake body
        int[] bodyX = new int[max_length];
        int[] bodyY = new int[max_length];
        int length = 1; // current length of snake
        bodyX[0] = width / 2; // start head position
        bodyY[0] = height / 2;
        int dx = 1, dy = 0; // direction shift
        int appleX;
        int appleY;

        do // put apple in empty cell
        {
            appleX = r.Next() % (width - 2) + 1;
            appleY = r.Next() % (height - 2) + 1;
        } while (appleX != bodyX[0] && appleY != bodyY[0]);

        for (int y = 0; y < height; y++) // print border
            for (int x = 0; x < width; x++)
                if (x == 0 && y == 0 || x == 0 && y == height - 1
                    || y == 0 && x == width - 1
                    || y == height - 1 && x == width - 1
                    || y == 0 || y == height - 1 || x == 0
                    || x == width - 1)
                    PutChar(x, y, ConsoleColor.DarkRed, wall);

        PutChar(appleX, appleY, ConsoleColor.Red, apple);
        PutChar(bodyX[0], bodyY[0], ConsoleColor.Green, head);

        do // собственно цикл игры
        {
            Thread.Sleep(pause);

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.DownArrow:
                        dy = 1;
                        dx = 0;
                        break;
                    case ConsoleKey.UpArrow:
                        dy = -1;
                        dx = 0;
                        break;
                    case ConsoleKey.LeftArrow:
                        dy = 0;
                        dx = -1;
                        break;
                    case ConsoleKey.RightArrow:
                        dy = 0;
                        dx = 1;
                        break;
                }
            }

            // snake head position after movement
            int X = bodyX[length - 1] + dx;
            int Y = bodyY[length - 1] + dy;

            if (X == 0 || X == width - 1 || Y == 0 || Y == height - 1) // hit borders
            {
                break; // game over
            }

            else if (X == appleX && Y == appleY) // apple intersect
            {
                PutChar(X, Y, ConsoleColor.Green, head); // print head of snake
                PutChar(bodyX[length - 1], bodyY[length - 1], ConsoleColor.Green, snake); // print snake body in "old" head position
                bodyX[length] = X;
                bodyY[length] = Y;
                length++;

                if (length == max_length) // проверка, достигла ли длина "змейки" своего максимального значения
                {
                    // Console.Clear();
                    Console.Title = "SNAKE WINS!";
                    Thread.Sleep(10000);
                    break;
                }

                // generate new apple position
                while (true)
                {
                    appleX = r.Next(1, width - 2);
                    appleY = r.Next(1, height - 2);

                    bool success = true;
                    for (int i = 0; i < length; i++) // apple can not be at snake body coords
                        if (appleX == bodyX[i] && appleY == bodyY[i])
                            success = false;

                    if (success) break;
                }

                PutChar(appleX, appleY, ConsoleColor.Red, apple);
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else // snake biting herself
            {
                bool bite = false;
                for (int i = 1; i < length; i++)
                    if (X == bodyX[i] &&
                        Y == bodyY[i])
                        bite = true;

                if (bite) // oops
                {
                    break;
                }
                else // snake body shifting 
                {
                    PutChar(bodyX[0], bodyY[0], ConsoleColor.Red, ' ');

                    if (length > 1)
                        PutChar(bodyX[length - 1], bodyY[length - 1], ConsoleColor.Green, snake);

                    for (int i = 0; i < length - 1; i++)
                    {
                        bodyX[i] = bodyX[i + 1];
                        bodyY[i] = bodyY[i + 1];
                    }

                    bodyX[length - 1] = X; // new head position
                    bodyY[length - 1] = Y;

                    PutChar(X, Y, ConsoleColor.Green, head);
                }
            }
        } while (true);

        // Console.Clear();
        Console.Title = "GAME OVER!";
        Thread.Sleep(10000);
    }

    public static void AutomatedSnake()
    {
        int max_length = (width - 2) * (height - 2);
        int[] bodyX = new int[max_length];
        int[] bodyY = new int[max_length];
        int length = 1;
        bodyX[0] = 1;
        bodyY[0] = 1;
        int dx = 1, dy = 0;
        int appleX;
        int appleY;

        do
        {
            appleX = r.Next() % (width - 2) + 1;
            appleY = r.Next() % (height - 2) + 1;
        } while (appleX != bodyX[0] && appleY != bodyY[0]);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (x == 0 && y == 0 || x == 0 && y == height - 1
                    || y == 0 && x == width - 1
                    || y == height - 1 && x == width - 1
                    || y == 0 || y == height - 1 || x == 0
                    || x == width - 1)
                    PutChar(x, y, ConsoleColor.DarkRed, wall);

        PutChar(appleX, appleY, ConsoleColor.Red, apple);
        PutChar(bodyX[0], bodyY[0], ConsoleColor.Green, head);

        int step_in_cycle = 1;

        HamiltonianCycle.grid = Pad(HamiltonianCycle.grid);

        do
        {
            Thread.Sleep(pause);

            ConsoleKey direction = ConsoleKey.RightArrow;

            int posx = bodyX[length - 1];
            int posy = bodyY[length - 1];
            int next = step_in_cycle + 1;

            if (HamiltonianCycle.grid[posy, posx + 1] == next) direction = ConsoleKey.RightArrow;
            else if (HamiltonianCycle.grid[posy, posx - 1] == next) direction = ConsoleKey.LeftArrow;
            else if (HamiltonianCycle.grid[posy + 1, posx] == next) direction = ConsoleKey.DownArrow;
            else if (HamiltonianCycle.grid[posy - 1, posx] == next) direction = ConsoleKey.UpArrow;
            else direction = ConsoleKey.UpArrow;

            step_in_cycle++;
            if (step_in_cycle >= (width - 2) * (height - 2))
                step_in_cycle = 0;

            switch (direction)
            {
                case ConsoleKey.DownArrow:
                    dy = 1;
                    dx = 0;
                    break;
                case ConsoleKey.UpArrow:
                    dy = -1;
                    dx = 0;
                    break;
                case ConsoleKey.LeftArrow:
                    dy = 0;
                    dx = -1;
                    break;
                case ConsoleKey.RightArrow:
                    dy = 0;
                    dx = 1;
                    break;
            }

            int X = bodyX[length - 1] + dx;
            int Y = bodyY[length - 1] + dy;

            if (X == 0 || X == width - 1 || Y == 0 || Y == height - 1)
            {
                break;
            }

            else if (X == appleX && Y == appleY)
            {
                PutChar(X, Y, ConsoleColor.Green, head);
                PutChar(bodyX[length - 1], bodyY[length - 1], ConsoleColor.Green, snake);
                bodyX[length] = X;
                bodyY[length] = Y;
                length++;

                if (length == max_length) // проверка, достигла ли длина "змейки" своего максимального значения
                {
                    Console.WriteLine("SNAKE WIN!");
                    Thread.Sleep(10000);
                    break;
                }

                while (true)
                {
                    appleX = r.Next(1, width - 1);
                    appleY = r.Next(1, height - 1);

                    bool success = true;
                    for (int i = 0; i < length; i++)
                    {
                        if (appleX == bodyX[i] && appleY == bodyY[i])
                        {
                            success = false;
                            break;
                        }
                    }
                    if (success) break;
                }

                PutChar(appleX, appleY, ConsoleColor.Red, apple);
                Console.ForegroundColor = ConsoleColor.Green;
            }

            else // не стена и не яблоко (проверка на укус тела)
            {
                bool bite = false;
                for (int i = 1; i < length; i++) // запуск цикла на сверку совпадений
                    if (X == bodyX[i] &&
                        Y == bodyY[i]) // если совпадение найдено
                        bite = true;

                if (bite)
                {
                    break;
                }
                else // а иначе запускаем обработку сдвига "змейки"
                {
                    PutChar(bodyX[0], bodyY[0], ConsoleColor.Red, ' ');

                    if (length > 1) // если длина змейки больше 
                        PutChar(bodyX[length - 1], bodyY[length - 1], ConsoleColor.Green, snake);

                    for (int i = 0; i < length - 1; i++) // запускаем цикл свдига координат звеньев "змейки"
                    {
                        bodyX[i] = bodyX[i + 1]; // обрабатываем все звенья - кроме последнего
                        bodyY[i] = bodyY[i + 1];
                    }

                    bodyX[length - 1] = X; // устанавливаем новую позицию головы "змейки"
                    bodyY[length - 1] = Y;

                    PutChar(X, Y, ConsoleColor.Green, head);
                }
            }
        } while (true);

        Console.Clear();
    }

    static void Main()
    {
        Options();

        Console.WriteLine("How do you want to play?");
        Console.WriteLine("1 - By Yourself");
        Console.WriteLine("2 - Automated Mode");
        int choice = Convert.ToInt32(Console.ReadLine());
        Console.Clear();
        if (choice == 1)
        {
            pause = 100;
            SimpleSnake();
        }
        else
        {
            HamiltonianCycle.Create(width, height);
            AutomatedSnake();
        }
    }
}