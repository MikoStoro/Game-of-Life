// See https://aka.ms/new-console-template for more information
using GameOfLifeBackend;

Board b = new Board(10, 10);
b.SetCellAlive(0, 0);
b.SetCellAlive(0,1);
b.SetCellAlive(1, 0);
b.DebugPrint();
b.NextStep();
b.DebugPrint();
b.NextStep();
b.DebugPrint();
b.NextStep();
b.DebugPrint();
