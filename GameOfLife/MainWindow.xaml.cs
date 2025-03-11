using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GameOfLifeBackend;
using Microsoft.Win32;
namespace GameOfLife
{
    public class VisualHost : FrameworkElement
    {
        // Create a collection of child visual objects.
        public DrawingVisual vis;

        public void ResetVisual()
        {

            vis = new DrawingVisual();
        }

        private void AddVisualToTree(object sender, RoutedEventArgs e)
        {
            AddVisualChild(vis);
            AddLogicalChild(vis);
        }

        private void RemoveVisualFromTree(object sender, RoutedEventArgs e)
        {
            RemoveLogicalChild(vis);
            RemoveVisualChild(vis);
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return vis;
        }

        public VisualHost()
        {
            vis = new DrawingVisual();
            Loaded += AddVisualToTree;
            Unloaded += RemoveVisualFromTree;

        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       

        Board board;
        VisualHost host;
        double cellSize = 10;

        int boardWidth = 1000;
        int boardHeight = 1000;
        int widthInCells = 100;
        int heightInCells = 100;

        int startX = 0;
        int startY = 0;

        int timePerTurn = 3;
        bool autoplay = false;
        Task autoPlayer;

        //STYLE
        delegate void DrawCell(DrawingContext ctx, double x, double y, double w, double h);
        DrawCell drawCell;
        Brush cellBrush;
        Pen cellOutline;
        Brush backgroundBrush;

        Dictionary<string,DrawCell> availableShapes = new ();
        Dictionary<string,Color> availableColors = new ();


        public MainWindow()
        {
            InitializeComponent();
            CreateBoard();
            //periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            //StartTimerAsync();
            board.SetCellAlive(0, 0);
            board.SetCellAlive(1, 0);
            board.SetCellAlive(2, 0);
            board.SetCellAlive(0, 1);
            board.SetCellAlive(0, 2);
            board.SetCellAlive(1, 1);

            autoPlayer = new Task(AutoplayerFunc);
            autoPlayer.Start();
            host = new VisualHost();
            BoardCanvas.Children.Add(host);

            cellBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            cellBrush.Freeze();
            backgroundBrush = new SolidColorBrush(Color.FromRgb(0,0,0));
            backgroundBrush.Freeze();
            cellOutline = new Pen();
            cellOutline.Freeze();
            drawCell = DrawEllipse;

            availableColors["Red"] = Color.FromRgb(255, 0, 0);
            availableColors["Green"] = Color.FromRgb(0, 255, 0);
            availableColors["Blue"] = Color.FromRgb(0, 0, 255);
            availableColors["White"] = Color.FromRgb(255, 255, 255);
            availableColors["Black"] = Color.FromRgb(0, 0, 0);

            availableShapes["Circle"] = DrawEllipse;
            availableShapes["Square"] = DrawRectangle;

            foreach (string item in availableColors.Keys)
            {
                InputBgColor.Items.Add(item);
                InputCellColor.Items.Add(item);
            }
            InputBgColor.SelectedIndex = 4; InputCellColor.SelectedIndex = 2;
            foreach(string item in availableShapes.Keys){
                InputCellShape.Items.Add(item);
            }
            InputCellShape.SelectedIndex = 1;
            
          

        }


        void DrawEllipse(DrawingContext ctx, double x, double y, double w, double h)
        {
            ctx.DrawEllipse(cellBrush, cellOutline, new Point(x+(w/2), y+(h/2)), w / 2, h / 2);
        }

        void DrawRectangle(DrawingContext ctx, double x, double y, double w, double h)
        {
            ctx.DrawRectangle(cellBrush, cellOutline, new Rect(x, y,w,h));
        }

        void AutoplayerFunc()
        {
            int ticks = 0;
            while (true)
            {
                Thread.Sleep(1000);
                if (autoplay)
                {
                    ticks++;
                    if (ticks % timePerTurn == 0)
                    {
                        ticks = 0;
                        Trace.WriteLine("Next turn called");
                        this.Dispatcher.Invoke(NextTurn);
                    }
                }
            }
        }

        public void CreateBoard()
        {
            var minn = int.Parse(InputMinNeighbours.Text);
            var maxn = int.Parse(InputMaxNeighbours.Text);
            var mins = int.Parse(InputMinNeighboursToSpawn.Text);
            var maxs = int.Parse(InputMaxNeighboursToSpawn.Text);
            boardWidth = int.Parse(InputBoardWidth.Text);
            boardHeight = int.Parse(InputBoardHeight.Text);
            board = new Board(boardWidth, boardHeight, minn, maxn, mins, maxs);
            SliderXPos.Maximum = boardWidth;
            SliderYPos.Maximum = boardHeight;
            calculateWidthInCells();
            SliderYPos.Value = 0;
            SliderXPos.Value = 0;
            
        }

        public void DrawBoard()
        {
            BoardCanvas.Children.Clear();
            host = new VisualHost();
            BoardCanvas.Children.Add(host);
            calculateWidthInCells();

            //double drawStart = (BoardCanvas.Width - (widthInCells * cellSize)) / 2;
            double drawStart = 0;
          /*  var greenBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            var blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            var whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            var pen = new Pen(thickness: cellSize * 0.01, brush: whiteBrush);
            blackBrush.Freeze();
            greenBrush.Freeze();
            whiteBrush.Freeze();
            pen.Freeze();*/



            using (DrawingContext ctx = host.vis.RenderOpen())
            {
                ctx.DrawRectangle(backgroundBrush, null,
                    new Rect(
                    drawStart,
                    drawStart,
                    widthInCells * cellSize + drawStart,
                    heightInCells * cellSize + drawStart));
                for (int x = 0; x < widthInCells; x++)
                {
                    for (int y = 0; y < heightInCells; y++)
                    {

                        //var rect = new System.Windows.Shapes.Rectangle();
                       if(board.GetCell(x + startX, y + startY) == cell.alive)
                        {
                            var xPos = x * cellSize + drawStart;
                            var yPos = y * cellSize + drawStart;
                            //rect.Stroke = new SolidColorBrush(Color.FromRgb(155, 155, 155));
                            //rect.StrokeThickness = cellSize * 0.01;

                            var width = cellSize; var height = cellSize;
                            /* var rect = new System.Windows.Rect(xPos, yPos, width, height);
                              var color = board.GetCell(x + startX, y + startY) == cell.alive ? greenBrush : blackBrush;
                                            ctx.DrawRectangle(color, pen, rect);*/

                            drawCell(ctx, xPos, yPos, width, height);
                        }
                       

                        //Canvas.SetTop(rect, yPos);
                        //Canvas.SetLeft(rect, xPos);
                        //BoardCanvas.Children.Add(rect);

                    }
                }
                ctx.DrawDrawing(host.vis.Drawing);
            }
            host.InvalidateVisual();

            BoardCanvas.UpdateLayout();
        }

        private void UpdateBoardColors()
        {
            var blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            var greenBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));

            for (int x = 0; x < widthInCells; x++)
            {
                for (int y = 0; y < heightInCells; y++)
                {
                    var brush = board.GetCell(x + startX, y + startY) == cell.alive ? greenBrush : blackBrush;
                    Rectangle rect = (Rectangle)BoardCanvas.Children[x * widthInCells + y];
                    rect.Fill = brush;
                }
            }
        }

        private void UpdatePosSlidersRange()
        {


            if (SliderXPos == null || SliderYPos == null) { return; }
            int xMax = boardWidth - widthInCells;
            int yMax = boardHeight - heightInCells;
            SliderXPos.Maximum = xMax;
            SliderYPos.Maximum = yMax;
            if (SliderXPos.Value > xMax)
            {
                SliderXPos.Value = xMax;
                SetXPos((int)xMax);
            }
            if(SliderYPos.Value > yMax)
            {
                SliderYPos.Value = yMax;
                SetYPos((int)yMax);
            }   
        }

        private void calculateWidthInCells()
        {
            widthInCells = (int)(BoardCanvas.Width / cellSize);
            heightInCells = (int)(BoardCanvas.Width / cellSize);
            if (widthInCells > boardWidth)
                widthInCells = boardWidth;
            if (heightInCells > boardHeight)
                heightInCells = boardHeight;
        }

        private void CellSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cellSize = CellSizeSlider.Value;

            calculateWidthInCells();

            if (board != null)
                DrawBoard();
            UpdatePosSlidersRange();
        }

        private void SetXPos(double x)
        {
            startX = (int)x;
            if (board != null)
                //UpdateBoardColors();
                DrawBoard();
        }

        private void SetYPos(double y)
        {
            startY = (int)y;
            if (board != null)
                //UpdateBoardColors();
                DrawBoard();
        }

        private void SliderXPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetXPos(SliderXPos.Value);
        }

        private void SliderYPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetYPos(SliderYPos.Value);
        }

        private void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            CreateBoard();
            int cellNum = int.Parse(InputNumberOfCells.Text);
            for(int i = 0; i < cellNum; i++)
            {
                if(i % 100==0)
                    Trace.WriteLine(i.ToString() + "/" + cellNum.ToString());
                int x = rnd.Next(0, boardWidth);
                int y = rnd.Next(0, boardHeight);
                board.SetCellAlive(x, y);
            }

           UpdatePosSlidersRange();


            //DrawBoard();
            DrawBoard();
            UpdateStats();
        }

        private void NextTurn()
        {
            board.NextStep();
            //UpdateBoardColors();
            DrawBoard();
            UpdateStats();
        }

        private void BtnNextTurn_Click(object sender, RoutedEventArgs e)
        {
            NextTurn();
        }

        private void UpdateStats()
        {
            DisplayBirths.Content = board.birthCount.ToString();
            DisplayDeaths.Content = board.deathCount.ToString();
            DisplayGeneration.Content = board.generationNo.ToString();
            DisplayAlive.Content = (board.birthCount - board.deathCount).ToString();
        }

        private void BtnSpeedx1_Click(object sender, RoutedEventArgs e)
        {
            timePerTurn = 3;
        }

        private void BtnSpeedx2_Click(object sender, RoutedEventArgs e)
        {
            timePerTurn = 1;
        }

     /*   private void ClockTick()
        {

        }*/

/*
        private async Task StartTimerAsync()
        {
            while (await periodicTimer.WaitForNextTickAsync())
            {
                if (autoplay)
                {
                    timer += 1;
                    if (timer % timePerTurn == 0)
                    {
                        timer = 0;
                        lock (turnLock)
                        {
                            this.Dispatcher.Invoke(NextTurn);
                        }

                    }
                }
            }
        }*/

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (autoplay == true)
            {
                BtnPlayPause.Content = "Play";
                autoplay = false;
                BtnNextTurn.IsEnabled = true;
            }
            else
            {
                autoplay = true;
                BtnPlayPause.Content = "Pause";
                BtnNextTurn.IsEnabled = false;
            }
        }

        private void ChangeCanvasState(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(BoardCanvas);
            int x = (int)(pos.X / cellSize) + startX;
            int y = (int)(pos.Y / cellSize) + startY;
            board.SwitchCell(x, y);
            UpdateStats();
            DrawBoard();
            //UpdateBoardColors();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var saveFileContent = board.GetSaveString();
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.ShowDialog();
            try
            {
                var stream = sfd.OpenFile();
                var writer = new StreamWriter(stream);
                writer.Write(saveFileContent);
                writer.Close();
                stream.Close();
            }catch { }

        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            var fileContent = string.Empty;
            if (ofd.ShowDialog() == true)
            {
                var filePath = ofd.FileName;

                var fileStream = ofd.OpenFile();

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    fileContent = reader.ReadToEnd();
                }
                board = (new Board.BoardLoader()).LoadBoard(fileContent);

                boardWidth = board.Width;
                boardHeight = board.Height;

                SliderXPos.Maximum = boardWidth;
                SliderYPos.Maximum = boardHeight;
                calculateWidthInCells();
                SliderYPos.Value = 0;
                SliderXPos.Value = 0;
                DrawBoard();
                UpdateStats();
            }
        }

        private void InputCellShape_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            drawCell = availableShapes[(string)InputCellShape.SelectedItem];
            DrawBoard();
        }

        private void InputCellColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cellBrush = new SolidColorBrush(availableColors[(string)InputCellColor.SelectedItem]);
            cellBrush.Freeze();
            DrawBoard();
        }

        private void InputBgColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            backgroundBrush = new SolidColorBrush(availableColors[(string)InputBgColor.SelectedItem]);
            backgroundBrush.Freeze();
            DrawBoard();
        }

        public void SaveImage()
        {
            int width = (int)(widthInCells * cellSize);
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, width, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(host.vis);
            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            sfd.ShowDialog();
            try
            {
                using (Stream stm = sfd.OpenFile())
                {
                    png.Save(stm);
                }
            }
            catch (Exception ex) { }
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveImage();
        }
    }

  

}