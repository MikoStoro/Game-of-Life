using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

namespace GameOfLifeBackend
{
    public enum cell { dead = 0, alive = 1, none = 2}
    public class CoordComparer : IEqualityComparer<Coordinates>
    {
        public bool Equals(Coordinates? x, Coordinates? y)
        {
            return (x.x == y.x && x.y == y.y);
        }

        public int GetHashCode([DisallowNull] Coordinates obj)
        {
            return (int)((obj.x * uint.MaxValue + obj.y) - uint.MaxValue/2);
        }
    }

    public class Coordinates
    {
        public uint x;
        public uint y;
        public Coordinates(int x, int y)
        {
            this.x = (uint)x;
            this.y = (uint)y;
        }

        public override string ToString()
        {
            return "(" + x.ToString() + " " + y.ToString() + ")";
        }
    }


    public class Board
    { 

        cell[,] board = null;
        int width = 0;
        int height = 0;
        
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        HashSet<Coordinates> liveFields;
        HashSet<Coordinates> emptyNeighbors;
        


        int _deathCount = 0;
        int _birthCount = 0;
        int _generationNo = 0;

        int minNeighbours = 2;
        int maxNeighbours = 3;
        int minCellsToSpawn = 3;
        int maxCellsToSpawn = 3;

        public int deathCount { get { return _deathCount; } }
        public int birthCount { get { return _birthCount; } }
        public int generationNo { get { return _generationNo; } }

        public Board(int w, int h, int min = 2, int max = 3, int mins = 3, int maxs = 3)
        {
            this.width = w;
            this.height = h;
            this.minNeighbours = min;
            this.maxNeighbours = max;
            this.minCellsToSpawn = mins;
            this.maxCellsToSpawn = maxs;

            this.board = new cell[w, h];
            liveFields = new HashSet<Coordinates>(new CoordComparer());
            emptyNeighbors = new HashSet<Coordinates>(new CoordComparer());
            liveFields.EnsureCapacity((w * h) / 4);
            emptyNeighbors.EnsureCapacity((w * h) / 4);
            /*            Coordinates.setMaxValue(w > h ? w : h);*/

            for (int i = 0; i < w * h; i++) board[i % w, i / h] = cell.dead;
        }

        protected void CorrectBirthCount()
        {
            _birthCount -= 1;
        }

        public void SetCellAlive(int x, int y)
        {
            board[x, y] = cell.alive;
            var coord = new Coordinates(x, y);
            if (!liveFields.Contains(coord))
                _birthCount += 1;
            liveFields.Add(coord);

            emptyNeighbors.Remove(coord);

            foreach(var c in GetNeighbours(x, y)){
                if(!liveFields.Contains(c))
                    emptyNeighbors.Add(c);
            }
        }

        public void SwitchCell(Coordinates c)
        {
            SwitchCell((int)c.x, (int)c.y);
        }

        public void SwitchCell(int x, int y)
        {   
            if(OutOfBounds(x, y)) return;
            var c = GetCell(x, y);
            if (c == cell.alive){SetCellDead(x, y);}
            if (c == cell.dead){SetCellAlive(x, y);}
        }

        public void SetCellDead(int x, int y)
        {
            if (OutOfBounds(x, y)) return ;
            board[x, y] = cell.dead;

            if(liveFields.Contains(new Coordinates(x, y)))
                _deathCount += 1;

            liveFields.Remove(new Coordinates(x, y));
        }

        public bool OutOfBounds(int x, int y)
        {
            if( x < 0 || y < 0 || x >= width || y >= height){
                return true;
            }
            return false;
        }

        public cell GetCell(int x, int y)
        {
            if (OutOfBounds(x, y)) return cell.none;
            return board[x, y];
        }

        public List<Coordinates> GetNeighbours(int x, int y)
        {
            var res = new List<Coordinates>();
            int dx = 1; int dy = 0;
            if(!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = -1; dy = 0;
            if(!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = 0; dy = 1;
            if (!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = 0; dy = -1;
            if (!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = 1; dy = 1;
            if (!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = -1; dy = 1;
            if (!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = -1; dy = -1;
            if (!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
            dx = 1; dy = -1;
            if (!OutOfBounds(x+dx, y + dy)){ res.Add(new Coordinates(x + dx, y+dy)); }
        
            return res;
        }

        public int GetNumOfNeighbours(int x, int y)
        {
            int n = 0;
            foreach(var c in GetNeighbours(x, y))
            {
               if (GetCell((int)c.x,(int)c.y) == cell.alive){ n++; }
            }
            return n;
        }

        public int GetNumOfNeighbours(Coordinates c)
        {
            return GetNumOfNeighbours((int)c.x,(int)c.y);
        }

        public void NextStep()
        {
            _generationNo += 1;
            var fieldsToSwitch = new HashSet<Coordinates>();
            foreach (var field in liveFields)
            {
                var n = GetNumOfNeighbours(field);
                if (n<minNeighbours || n > maxNeighbours){
                    fieldsToSwitch.Add(field);
                }
            }

            var emptyNeghborsToRemove = new HashSet<Coordinates>();
            foreach (var field in emptyNeighbors)
            {
                var n = GetNumOfNeighbours(field);
                if (n == 0) {
                    emptyNeghborsToRemove.Add(field); }
                else if ( n >= minCellsToSpawn && n <= maxCellsToSpawn){
                    fieldsToSwitch.Add(field);
                }
            }

            foreach (var field in emptyNeghborsToRemove){
                emptyNeighbors.Remove(field);
            }
            foreach (var field in fieldsToSwitch){
                SwitchCell(field);
            }
        }

        public void DebugPrint()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int v = (board[x, y] == cell.alive) ? 1 : 0;
                    Console.Write(v.ToString());
                }
                Console.Write("\n");
            }

            foreach(var f in liveFields)
            {
                Console.WriteLine(f.ToString());
            }
        }

        public string GetSaveString()
        {
            string res = "";
            res += minNeighbours.ToString() + "\n";
            res += maxNeighbours.ToString() + "\n";
            res += minCellsToSpawn.ToString() + "\n";
            res += maxCellsToSpawn.ToString() + "\n";

            res += generationNo.ToString() + "\n";
            res += birthCount.ToString() + "\n";
            res += deathCount.ToString() + "\n";

            res += width + " " + height + "\n";

            for(int x = 0; x < width; x++){
                for(int y = 0; y < height; y++){
                    res += board[x, y] == cell.alive ? "1" : "0";
                }
                res += "\n";
            }

            return res;
        }

        protected void SetDeathCount(int d)
        {
            _deathCount = d;
        }

        protected void SetBirthCount(int b)
        {
            _birthCount = b;
        }
        protected void SetGenNo(int g)
        {
            _generationNo = g;
        }


        public class BoardLoader
        {
            public BoardLoader() { }

            public Board LoadBoard(string config)
            {
                var configArr = config.Split('\n');
                int minn = int.Parse(configArr[0]);
                int maxn = int.Parse(configArr[1]);
                int mins = int.Parse(configArr[2]);
                int maxs = int.Parse(configArr[3]);

                int generation = int.Parse(configArr[4]);
                int births = int.Parse(configArr[5]);
                int deaths = int.Parse(configArr[6]);

                int w = int.Parse(configArr[7].Split(" ")[0]);
                int h = int.Parse(configArr[7].Split(" ")[1]);

                Board b = new Board(w, h, minn, maxn, mins, maxs);
                b.SetDeathCount(deaths);
                b.SetBirthCount(births);
                b.SetGenNo(generation);

                for(int y = 0; y < h; y++){
                    var line = configArr[y + 8].ToCharArray();
                    for(int x = 0; x < w; x++){
                        if (line[x] == '1'){
                            b.SetCellAlive(x, y);
                            b.CorrectBirthCount();
                        }
                    }
                }

                return b;
            }
        }

    }
}
