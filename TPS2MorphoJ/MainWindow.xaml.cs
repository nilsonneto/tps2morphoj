using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace TPS2MorphoJ
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TPSComplex parsed = new TPSComplex
        {
            scale = 0,
            images = new List<TPSImage>()
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FindPath_Click(object sender, RoutedEventArgs e)
        {
            int size = -1;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            var result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result is bool && result == true) // Test result.
            {
                string file = openFileDialog1.FileName;
                PathTPS.Text = file;
                try
                {
                    string text = File.ReadAllText(file);
                    OriginalTPS.Text = text;
                    size = text.Length;
                }
                catch (IOException)
                {
                    Console.WriteLine("Ops.");
                }
            }
            Console.WriteLine(size); // <-- Shows file size in debugging mode.
            Console.WriteLine(result); // <-- For debugging use.
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var allLines = OriginalTPS.Text.Split('\n');
            var scale = allLines.FirstOrDefault(l => l.StartsWith("SCALE"));
            if (scale != null)
            {
                parsed.scale = double.Parse(scale.Split('=')[1].Replace(',', '.'));
            }
            var noScales = string.Join("\n", allLines.Where(l => !l.StartsWith("SCALE")));
            String pattern = @"(?=LM=\d*)";
            var imagesStrings = Regex.Split(noScales, pattern);
            imagesStrings.ToList().ForEach(image =>
            {
                if (!string.IsNullOrWhiteSpace(image))
                {
                    var imageItem = new TPSImage
                    {
                        lmCount = 0,
                        lms = new List<LM>(),
                        curvesCount = 0,
                        curves = new List<PointSet>(),
                        ImageName = "",
                        ID = 0
                    };
                    var imageLines = image.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    for (var i = 0; i < imageLines.Length; i++)
                    {
                        var line = imageLines[i];
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            if (line.StartsWith("LM"))
                            {
                                imageItem.lmCount = int.Parse(line.Split('=')[1].Replace(',', '.'));
                                for (var y = 0; y < imageItem.lmCount; y++)
                                {
                                    i++; // skip to next line to consume lines
                                    imageItem.lms.Add(new LM
                                    {
                                        index = y,
                                        content = imageLines[i]
                                    });
                                }
                            }
                            else if (line.StartsWith("CURVES"))
                            {
                                imageItem.curvesCount = int.Parse(line.Split('=')[1].Replace(',', '.'));
                                for (var y = 0; y < imageItem.curvesCount; y++)
                                {
                                    i++; // skip to next line to consume lines
                                    var pointSet = new PointSet
                                    {
                                        index = y,
                                        pointsCount = int.Parse(imageLines[i].Split('=')[1].Replace(',', '.')),
                                        pointItems = new List<PointItem>()
                                    };
                                    for (var z = 0; z < pointSet.pointsCount; z++)
                                    {
                                        i++; // skip to next line to consume lines
                                        pointSet.pointItems.Add(new PointItem
                                        {
                                            index = z,
                                            content = imageLines[i]
                                        });
                                    }
                                    imageItem.curves.Add(pointSet);
                                }
                            }
                            else if (line.StartsWith("IMAGE"))
                            {
                                imageItem.ImageName = line.Split('=')[1];
                            }
                            else if (line.StartsWith("ID"))
                            {
                                imageItem.ID = int.Parse(line.Split('=')[1].Replace(',', '.'));
                            }
                        }
                    }
                    parsed.images.Add(imageItem);
                }
            });
            Console.Write(noScales);
        }
    }

    /// LM=x1
    /// ...
    /// IMAGE=str1
    /// ID=y1
    /// LM=x2
    /// ...
    /// IMAGE=str2
    /// ID=y2
    /// SCALE=z

    public class TPSComplex
    {
        public List<TPSImage> images;
        public double scale;
    }

    public class TPSImage
    {
        public int lmCount;
        public List<LM> lms;
        public int curvesCount;
        public List<PointSet> curves;
        public string ImageName;
        public int ID;
    }

    public class TPSSimple
    {
        public int lmCount;
        public List<LM> leafs;
        public string ImageName;
        public int ID;
    }

    public class LM
    {
        public int index;
        public string content;
    }

    public class PointSet
    {
        public int index;
        public int pointsCount;
        public List<PointItem> pointItems;
    }

    public class PointItem
    {
        public int index;
        public string content;
    }
}
