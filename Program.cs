using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Timers;
using System.Diagnostics;

namespace CrossesAndNils
{
    public class CrossesAndNils
    {
        int N = 3;

        int COUNT = 0;
        int CROSS_WIN = 0;
        int NILLS_WIN = 0;

        StreamWriter sw = null;
        private static Mutex _mutex = new Mutex();

        public CrossesAndNils()
        {
            var filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/CrossereAndNils_mt.txt";
            sw = File.CreateText(filename);
            System.Console.WriteLine(filename);
        }

        void print(int[,] board)
        {
            System.Console.Write("+");
            for (int j = 0; j < N; ++j)
            {
                System.Console.Write("-+");
            }
            System.Console.WriteLine();
            for (int i = 0; i < N; ++i)
            {
                System.Console.Write("|");
                for (int j = 0; j < N; ++j)
                {
                    switch (board[i, j])
                    {
                        case +1: System.Console.Write("x"); break;
                        case -1: System.Console.Write("o"); break;
                        default: System.Console.Write(" "); break;
                    }
                    System.Console.Write("|");
                }
                System.Console.WriteLine();
                System.Console.Write("+");
                for (int j = 0; j < N; ++j)
                {
                    System.Console.Write("-+");
                }
                System.Console.WriteLine();
            }
        }

        int evaluate(int[,] board)
        {
            // columns
            for (int j = 0; j < N; ++j)
            {
                int sum1 = 0;
                for (int i = 0; i < N; ++i)
                    sum1 += board[i, j];
                if (sum1 == +N) return +1;
                if (sum1 == -N) return -1;
            }
            // rows
            for (int i = 0; i < N; ++i)
            {
                int sum2 = 0;
                for (int j = 0; j < N; ++j)
                    sum2 += board[i, j];
                if (sum2 == +N) return +1;
                if (sum2 == -N) return -1;
            }
            // diags
            int sum = 0;
            for (int i = 0; i < N; ++i)
            {
                sum += board[i, i];
            }
            if (sum == +N) return +1;
            if (sum == -N) return -1;
            //
            sum = 0;
            for (int i = 0; i < N; ++i)
            {
                sum += board[i, N - i - 1];
            }
            if (sum == +N) return +1;
            if (sum == -N) return -1;
            return 0;
        }

        string get_string(int[,] board)
        {
            string ss = "";
            for (int i = 0; i < N; ++i)
            {
                ss += "[";
                for (int j = 0; j < N; ++j)
                {
                    switch (board[i, j])
                    {
                        case +1: ss += "x"; break;
                        case -1: ss += "o"; break;
                        default: ss += "_"; break;
                    }
                }
                ss += "]";
            }
            return ss;
        }

        int who_move(int[,] board)
        {
            int sum0 = 0;
            int sum1 = 0;
            int sum2 = 0;
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < N; ++j)
                {
                    var v = board[i, j];
                    if (v == 0) sum0++;
                    if (v == 1) sum1++;
                    if (v == -1) sum2++;
                }

            if (sum0 == 0) return 0;
            if (sum1 <= sum2)
                return 1;
            else
                return -1;
        }


        void copy(int[,] src, int[,] dst)
        {
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < N; ++j)
                    dst[i, j] = src[i, j];
        }

        bool move(int[,] board, string dir)
        {
            string s = get_string(board);
            int who_win = evaluate(board);
            dir = dir + '/';
            foreach (var ch in s)
            {
                var ch1 = ch;
                switch (ch)
                {
                    case 'x': if (who_win == +1) ch1 = 'X'; break;
                    case 'o': if (who_win == -1) ch1 = 'O'; break;
                    default: break;
                }
                dir = dir + ch1;
            }

            _mutex.WaitOne();
            //System.Console.WriteLine(s);
            //System.Console.WriteLine(dir);
            sw.WriteLine(dir);
            _mutex.ReleaseMutex();

            //if (!Directory.Exists(dir))
            //{
            //    DirectoryInfo di = Directory.CreateDirectory(dir);
            //    //Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(dir));
            //}

            Interlocked.Increment(ref COUNT);
            switch (who_win)
            {
                case +1: Interlocked.Increment(ref CROSS_WIN); return false;
                case -1: Interlocked.Increment(ref NILLS_WIN); return false;
            }
            int who = who_move(board);
            bool moved = false;
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < N; ++j)
                    if (board[i, j] == 0)
                    {
                        int[,] board1 = new int[3, 3];
                        copy(board, board1);
                        board1[i, j] = who;
                        moved = true;
                        move(board1, dir);
                    }
            return moved;
        }


        private void ThreadProcess(int i, int j)
        {
            //_mutex.WaitOne();
            //System.Console.WriteLine("Run{0}{1}", i, j);
            //_mutex.ReleaseMutex();

            int[,] board = new int[N, N];
            board[i, j] = 1;
            bool res = move(board, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public void Run()
        {
            int[,] board = new int[N, N];

            print(board);
            System.Console.WriteLine(get_string(board));
            System.Console.WriteLine();

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // single thread
            //bool res = move(board, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            Thread[,] threadsArray = new Thread[N, N];
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    //System.Console.WriteLine("{0}{1}", i, j);
                    int ii = i;
                    int jj = j;
                    threadsArray[i, j] = new Thread(() => ThreadProcess(ii, jj));
                    threadsArray[i, j].Name = String.Format("Thread{0}{1}", i, j);
                    threadsArray[i, j].Start();
                }


            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    threadsArray[i, j].Join();
                }

            timer.Stop();
            System.Console.WriteLine(timer.Elapsed.ToString());

            System.Console.WriteLine(COUNT);       // 549946
            System.Console.WriteLine(CROSS_WIN);   // 131184
            System.Console.WriteLine(NILLS_WIN);   // 77904
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            //int[,] board = new int[3, 3];
            //foreach (var v in board)
            //{
            //    System.Console.WriteLine(v);
            //}

            CrossesAndNils a = new CrossesAndNils();
            a.Run();
        }
    }
}
