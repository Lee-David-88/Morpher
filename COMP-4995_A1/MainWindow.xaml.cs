using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace COMP_4995_A1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class linePair
    {
        public linePair(Line DLine, Line SLine, int id)
        {
            DestinationControlLine = DLine;
            SourceControlLine = SLine;
            Id = id;
        }
        public int Id;
        public Line DestinationControlLine;
        public Line SourceControlLine;
    }

    public partial class MainWindow : Window
    {

        Point start;
        Point currentStartPoint;


        bool isPressed;
        bool isDrawing;
        bool isEditing;
        bool isEditLine;
        bool needSwap;

        Line line;
        Line rightLine;
        Line currentLine;

        BitmapImage leftBitMapImage;
        BitmapImage rightBitMapImage;

        int detectRange = 5;
        int lineNum = 0;
        int selectedLine;
        int totalFrame = 10;

        float a = 0.01f;
        float b = 2f;
        float p = 0.1f;

        List<linePair> lineList = new List<linePair>();
        List<byte[]> frameList = new List<byte[]>();


        public MainWindow()
        {
            InitializeComponent();
        }


        // Buttons
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                LeftImage.Source = new BitmapImage(fileUri);
                leftBitMapImage = new BitmapImage(fileUri);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                RightImage.Source = new BitmapImage(fileUri);
                rightBitMapImage = new BitmapImage(fileUri);
            }
        }

        private void DrawMode_Click(object sender, RoutedEventArgs e)
        {
            isEditing = false;
            isDrawing = true;
        }

        private void EditMode_Click(object sender, RoutedEventArgs e)
        {
            isDrawing = false;
            isEditing = true;
        }

        private void MorphImage(byte[] imageInLeft, byte[] imageInRight, int xMax, int yMax)
        {
            frameList.Clear();
            float currentFrame;
            int bytesPerPixel = 4;
            frameList.Add(imageInLeft);
            for (int f = 0; f < totalFrame; f++)
            {
                byte[] imageOutLeft = new byte[imageInLeft.Length];
                currentFrame = f / (float)totalFrame;
                for (int y = 0; y < yMax; y++)
                {
                    for (int x = 0; x < xMax; x++)
                    {
                        // Left Image;
                        double weightSumLeft = 0;
                        Point deltaTsLeft = new Point(0, 0);

                        Point T = new Point(x, y);
                        for (int l = 0; l < lineList.Count; l++)
                        {
                            Point P = new Point(lineList[l].SourceControlLine.X1, lineList[l].SourceControlLine.Y1);
                            Point Q = new Point(lineList[l].SourceControlLine.X2, lineList[l].SourceControlLine.Y2);
                            Point Pprime = new Point(lineList[l].DestinationControlLine.X1, lineList[l].DestinationControlLine.Y1);
                            Point Qprime = new Point(lineList[l].DestinationControlLine.X2, lineList[l].DestinationControlLine.Y2);

                            Point sourcePoint = getSourcePoint(P, Q, Pprime, Qprime, T);

                            Point currentDeltaT = new Point(sourcePoint.X - T.X, sourcePoint.Y - T.Y);
                            currentDeltaT = new Point(currentDeltaT.X * (currentFrame), currentDeltaT.Y * (currentFrame));

                            Vector PQ = CreateVector(P, Q);
                            Vector n = CreateNormal(PQ);
                            Vector PT = CreateVector(P, T);                            
                            Vector QT = CreateVector(Q, T);

                            //Vector TQ = CreateVector(T, Q);
                            //Vector TP = CreateVector(T, P);

                            double fl = ProjectionMagnitude(PT, PQ) / Magnitude(PQ);
                            double d = ProjectionMagnitude(PT, n);
                            if (fl < 0 || fl > 1)
                            {
                                d = MathF.Min((float)Magnitude(PT), (float)Magnitude(QT));
                            }
                            double currentWeight = CreateWeight(a, b, p, d, PQ);
                            deltaTsLeft = new Point(currentDeltaT.X * currentWeight + deltaTsLeft.X, currentDeltaT.Y * currentWeight + deltaTsLeft.Y);
                            weightSumLeft += currentWeight;
                        }

                        Point finalDeltaTLeft = new Point(T.X + (deltaTsLeft.X / weightSumLeft), T.Y + (deltaTsLeft.Y / weightSumLeft));

                        int pointXLeft = (int)Math.Clamp(finalDeltaTLeft.X, 0, xMax - 1);
                        int pointYLeft = (int)Math.Clamp(finalDeltaTLeft.Y, 0, yMax - 1);
                        int modIndexLeft = (y * xMax + x) * bytesPerPixel;
                        int sourceIndexLeft = (pointYLeft * xMax + pointXLeft) * bytesPerPixel;

                        // Right Image
                        double weightSumRight = 0;
                        Point deltaTsRight = new Point(0, 0);

                        for (int l = 0; l < lineList.Count; l++)
                        {
                            Point P = new Point(lineList[l].DestinationControlLine.X1, lineList[l].DestinationControlLine.Y1);
                            Point Q = new Point(lineList[l].DestinationControlLine.X2, lineList[l].DestinationControlLine.Y2);
                            Point Pprime = new Point(lineList[l].SourceControlLine.X1, lineList[l].SourceControlLine.Y1);
                            Point Qprime = new Point(lineList[l].SourceControlLine.X2, lineList[l].SourceControlLine.Y2);

                            Point sourcePoint = getSourcePoint(P, Q, Pprime, Qprime, T);

                            Point currentDeltaT = new Point(sourcePoint.X - T.X, sourcePoint.Y - T.Y);
                            currentDeltaT = new Point(currentDeltaT.X * (1-currentFrame), currentDeltaT.Y *(1-currentFrame));
                            Vector PQ = CreateVector(P, Q);
                            Vector n = CreateNormal(PQ);
                            Vector PT = CreateVector(P, T);
                            Vector QT = CreateVector(Q, T);

                            //Vector TQ = CreateVector(T, Q);
                            //Vector TP = CreateVector(T, P);

                            double fl = ProjectionMagnitude(PT, PQ) / Magnitude(PQ);
                            double d = ProjectionMagnitude(PT, n);
                            if (fl < 0 || fl > 1)
                            {
                                d = MathF.Min((float)Magnitude(PT), (float)Magnitude(QT));
                            }
                            
                            double currentWeight = CreateWeight(a, b, p, d, PQ);
                            deltaTsRight = new Point(currentDeltaT.X * currentWeight + deltaTsRight.X, currentDeltaT.Y * currentWeight + deltaTsRight.Y);
                            weightSumRight += currentWeight;
                        }

                        Point finalDeltaTRight = new Point(T.X + (deltaTsRight.X / weightSumRight), T.Y + (deltaTsRight.Y / weightSumRight));

                        int pointXRight = (int)Math.Clamp(finalDeltaTRight.X, 0, xMax - 1);
                        int pointYRight = (int)Math.Clamp(finalDeltaTRight.Y, 0, yMax - 1);
                        int sourceIndexRight = (pointYRight * xMax + pointXRight) * bytesPerPixel;

                        for (int i = 0; i < 4; i++)
                        {
                            imageOutLeft[modIndexLeft + i] = (byte)((imageInLeft[sourceIndexLeft + i] * (1-currentFrame)) + (imageInRight[sourceIndexRight + i] * (currentFrame)));
                        }
                    }
                }
                frameList.Add(imageOutLeft);
            }
            frameList.Add(imageInRight);
        }

        private void Morph_Click(object sender, RoutedEventArgs e)
        {
            byte[] destinationArray = BitmapSourceToArray(leftBitMapImage);
            byte[] sourceArray = BitmapSourceToArray(rightBitMapImage);
            int xMax = leftBitMapImage.PixelWidth;
            int yMax = leftBitMapImage.PixelHeight;
            MorphImage(destinationArray, sourceArray, xMax, yMax);

            Window1 window1 = new Window1(frameList, xMax, yMax);
            window1.Show();
        }

        private void Warp_Click(object sender, RoutedEventArgs e)
        {   
            byte[] destinationArray = BitmapSourceToArray(leftBitMapImage);
            int xMax = leftBitMapImage.PixelWidth;
            int yMax = leftBitMapImage.PixelHeight;

            RightImage.Source = BitmapSourceFromArray(WarpImage(destinationArray, xMax, yMax), leftBitMapImage.PixelWidth, leftBitMapImage.PixelHeight);
        }

        private byte[] WarpImage(byte[] imageIn, int xMax, int yMax)
        {
            int bytesPerPixel = 4;
            byte[] imageOut = new byte[imageIn.Length];
            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    double weightSum = 0;
                    Point deltaTs = new Point(0, 0);

                    Point T = new Point(x, y);
                    for (int l = 0; l < lineList.Count; l++)
                    {
                        Point P = new Point(lineList[l].SourceControlLine.X1, lineList[l].SourceControlLine.Y1);
                        Point Q = new Point(lineList[l].SourceControlLine.X2, lineList[l].SourceControlLine.Y2);
                        Point Pprime = new Point(lineList[l].DestinationControlLine.X1, lineList[l].DestinationControlLine.Y1);
                        Point Qprime = new Point(lineList[l].DestinationControlLine.X2, lineList[l].DestinationControlLine.Y2);

                        Point sourcePoint = getSourcePoint(P, Q, Pprime, Qprime, T);

                        Point currentDeltaT = new Point(sourcePoint.X - T.X, sourcePoint.Y - T.Y);
                        Vector PQ = CreateVector(P, Q);
                        Vector n = CreateNormal(PQ);
                        Vector PT = CreateVector(P, T);
                        double d = ProjectionMagnitude(PT, n);
                        double currentWeight = CreateWeight(a, b, p, d, PQ);
                        deltaTs = new Point(currentDeltaT.X * currentWeight + deltaTs.X, currentDeltaT.Y * currentWeight + deltaTs.Y);
                        weightSum += currentWeight;
                    }

                    Point finalDeltaT = new Point(T.X + (deltaTs.X / weightSum), T.Y + (deltaTs.Y / weightSum));

                    int pointX = (int)Math.Clamp(finalDeltaT.X, 0, xMax - 1);
                    int pointY = (int)Math.Clamp(finalDeltaT.Y, 0, yMax - 1);
                    int modIndex = (y * xMax + x) * bytesPerPixel;
                    int sourceIndex = (pointY * xMax + pointX) * bytesPerPixel;

                    for (int i = 0; i < 4; i++)
                    {
                        imageOut[modIndex + i] = imageIn[sourceIndex + i];
                    }
                }
            }
            return imageOut;
        }

        private Point getSourcePoint(Point P, Point Q, Point Pprime, Point Qprime, Point T)
        {   
            Vector PQ = CreateVector(P, Q);
            Vector n = CreateNormal(PQ);
            Vector PT = CreateVector(P, T);
            double d = ProjectionMagnitude(PT, n);
            double fl = ProjectionMagnitude(PT, PQ) / Magnitude(PQ);

            Vector PQprime = CreateVector(Pprime, Qprime);
            Vector nprime = CreateNormal(PQprime);

            Point Tprime =  Pprime +
                            new Vector(PQprime.X , PQprime.Y ) * fl +
                            new Vector(nprime.X / Magnitude(nprime), nprime.Y / Magnitude(nprime)) * d;

            return Tprime;
        }


        // Bitmap Convertion Functions
        private byte[] BitmapSourceToArray(BitmapSource bitmapSource)
        {
            int stride = (int)bitmapSource.PixelWidth * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        private BitmapSource BitmapSourceFromArray(byte[] pixels, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * (bitmap.Format.BitsPerPixel / 8), 0);

            return bitmap;
        }


        // Mouse Control Functions
        private void RightImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isEditing)
            {
                //Trace.WriteLine(RightDrawable.Children.Count);
                if (isEditLine)
                {
                    isEditLine = false;
                    lineList[selectedLine].SourceControlLine = currentLine;
                
                    currentLine = new Line();
                }
                else
                {   
                    for (int i = 0; i < lineList.Count; i++)
                    {
                        if (e.GetPosition(RightDrawable).X <= lineList[i].SourceControlLine.X1 + detectRange &&
                            e.GetPosition(RightDrawable).X >= lineList[i].SourceControlLine.X1 - detectRange &&
                            e.GetPosition(RightDrawable).Y <= lineList[i].SourceControlLine.Y1 + detectRange &&
                            e.GetPosition(RightDrawable).Y >= lineList[i].SourceControlLine.Y1 - detectRange)
                        {
                            currentStartPoint.X = lineList[i].SourceControlLine.X2;
                            currentStartPoint.Y = lineList[i].SourceControlLine.Y2;
                            RightDrawable.Children.Remove(lineList[i].SourceControlLine);
                            selectedLine = lineList[i].Id;
                            isEditLine = true;
                            needSwap = true;
                            break;
                        }
                        if (e.GetPosition(RightDrawable).X <= lineList[i].SourceControlLine.X2 + detectRange &&
                            e.GetPosition(RightDrawable).X >= lineList[i].SourceControlLine.X2 - detectRange &&
                            e.GetPosition(RightDrawable).Y <= lineList[i].SourceControlLine.Y2 + detectRange &&
                            e.GetPosition(RightDrawable).Y >= lineList[i].SourceControlLine.Y2 - detectRange)
                        {

                            currentStartPoint.X = lineList[i].SourceControlLine.X1;
                            currentStartPoint.Y = lineList[i].SourceControlLine.Y1;
                            RightDrawable.Children.Remove(lineList[i].SourceControlLine);
                            selectedLine = lineList[i].Id;

                            isEditLine = true;
                            break;
                        }
                    }
                }
            }
        }

        private void RightImage_MoveMouse(object sender, MouseEventArgs e)
        {
            if (isEditing)
            {
                if (isEditLine)
                {   
                    if (needSwap)
                    {
                        RightDrawable.Children.Remove(currentLine);
                        currentLine = new Line();
                        currentLine.X2 = currentStartPoint.X;
                        currentLine.Y2 = currentStartPoint.Y;
                        currentLine.X1 = e.GetPosition(RightDrawable).X;
                        currentLine.Y1 = e.GetPosition(RightDrawable).Y;
                        currentLine.Stroke = Brushes.Green;
                        currentLine.StrokeThickness = 2;
                        RightDrawable.Children.Add(currentLine);
                        currentLine.MouseUp += RightImage_MouseLeftButtonDown;
                    }
                    else
                    {
                        RightDrawable.Children.Remove(currentLine);
                        currentLine = new Line();
                        currentLine.X1 = currentStartPoint.X;
                        currentLine.Y1 = currentStartPoint.Y;
                        currentLine.X2 = e.GetPosition(RightDrawable).X;
                        currentLine.Y2 = e.GetPosition(RightDrawable).Y;
                        currentLine.Stroke = Brushes.Green;
                        currentLine.StrokeThickness = 2;
                        RightDrawable.Children.Add(currentLine);
                        currentLine.MouseUp += RightImage_MouseLeftButtonDown;
                    }
                }
            }

        }

        private void LeftImage_MoveMouse(object sender, MouseEventArgs e)
        {   
            if (isDrawing)
            {
                if (isPressed)
                {
                    LeftDrawable.Children.Remove(line);
                    line = new Line();
                    line.X1 = start.X;
                    line.Y1 = start.Y;
                    line.X2 = e.GetPosition(LeftDrawable).X;
                    line.Y2 = e.GetPosition(LeftDrawable).Y;
                    line.Stroke = Brushes.Green;
                    line.StrokeThickness = 2;
                    LeftDrawable.Children.Add(line);
                    line.MouseUp += LeftImage_MouseLeftButtonDown;
                }
            }
            else if (isEditing)
            {
                if (isEditLine)
                {
                    LeftDrawable.Children.Remove(currentLine);
                    currentLine = new Line();
                    currentLine.X1 = currentStartPoint.X;
                    currentLine.Y1 = currentStartPoint.Y;
                    currentLine.X2 = e.GetPosition(LeftDrawable).X;
                    currentLine.Y2 = e.GetPosition(LeftDrawable).Y;
                    currentLine.Stroke = Brushes.Green;
                    currentLine.StrokeThickness = 2;
                    LeftDrawable.Children.Add(currentLine);
                    currentLine.MouseUp += LeftImage_MouseLeftButtonDown;
                }
            }
            
        }

        private void LeftImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                if (isPressed)
                {
                    isPressed = false;

                    // Add Line to Right Canvas
                    rightLine = new Line();
                    rightLine.X1 = start.X;
                    rightLine.Y1 = start.Y;
                    rightLine.X2 = e.GetPosition(LeftDrawable).X;
                    rightLine.Y2 = e.GetPosition(LeftDrawable).Y;
                    rightLine.Stroke = Brushes.Green;
                    rightLine.StrokeThickness = 2;
                    RightDrawable.Children.Add(rightLine);

                    // Store Lines in List

                    linePair pair = new linePair(line, rightLine, lineNum);
                    lineList.Add(pair);
                    lineNum++;

                    line = new Line();
                }
                else
                {
                    start = e.GetPosition(LeftDrawable);
                    isPressed = true;
                }
            }
            else if (isEditing)
            {
                
                if (isEditLine)
                {
                    isEditLine = false;
                    currentLine = new Line();
                }
                else
                {
                    foreach (Line l in LeftDrawable.Children)
                    {
                        Trace.WriteLine(l);
                        Line line1 = l;
                        if (e.GetPosition(LeftDrawable).X == line1.X1)
                        {
                            Trace.WriteLine("Selected");
                            currentStartPoint.X = line1.X2;
                            currentStartPoint.Y = line1.Y2;
                            LeftDrawable.Children.Remove(line1);
                            isEditLine = true;
                            break;
                        }
                    }
                }
            }
        }


        // Math Functions
        private Vector CreateVector(Point p, Point q)
        {
            return new Vector(q.X - p.X, q.Y - p.Y);
        }

        private Vector CreateNormal(Vector v)
        {
            return new Vector(v.Y * -1, v.X);
        }

        private double Magnitude(Vector v)
        {
            return MathF.Sqrt(MathF.Pow((float)v.X, 2) + MathF.Pow((float)v.Y, 2));
        }

        double DotProduct(Vector a, Vector b)
        {
            return (a.X * b.X) + (a.Y * b.Y);
        }

        double ProjectionMagnitude(Vector top, Vector bot) 
        {
            return DotProduct(top, bot) / Magnitude(bot);
        }

        double CreateWeight(float a, float b, float p, double d, Vector PQ)
        {   
            return MathF.Pow(MathF.Abs(MathF.Pow((float)Magnitude(PQ), p)/ (a + MathF.Abs((float)d))), b);
        }
    }
}
