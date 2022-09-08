//using dotnetCHARTING;
using System;
using System.Drawing;
using System.IO;


namespace Export
{
    class Graph
    {
       /* SeriesCollection getRandomData()
        {
            SeriesCollection SC = new SeriesCollection();
            Random myR = new Random();
            for (int a = 0; a < 4; a++)
            {
                Series s = new Series();
                s.Name = "Series " + a;
                for (int b = 0; b < 7; b++)
                {
                    Element e = new Element();
                    e.Name = "Element " + b;
                    e.YValue = myR.Next(50);
                    s.Elements.Add(e);
                }
                SC.Add(s);
            }

            // Set Different Colors for our Series
            SC[0].DefaultElement.Color = Color.FromArgb(49, 255, 49);
            SC[1].DefaultElement.Color = Color.FromArgb(255, 255, 0);
            SC[2].DefaultElement.Color = Color.FromArgb(255, 99, 49);
            SC[3].DefaultElement.Color = Color.FromArgb(0, 156, 255);
            return SC;
        }*/

        internal static void GetGraph()
        {/*
            Chart chart = new();
            chart.Width = 800;
            chart.Height = 600;
            chart.Type = ChartType.Pie;
            chart.TitleBox.Label.Text = "World Population by Age Group";
            chart.ImageFormat = dotnetCHARTING.ImageFormat.Jpg;
            chart.SeriesCollection.Add(getRandomData());
            chart.TempDirectory = Path.GetTempPath();

            Bitmap bmp = chart.GetChartBitmap();
            chart.FileManager.SaveImage(bmp);
            bmp.Save("D:\\test.jpg");*/
        }
    }
}
