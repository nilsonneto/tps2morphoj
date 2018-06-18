using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Linq;
using MoreLinq;
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
            scale = "",
            images = new List<TPSImage>()
        };

        private MorphoJFormat output = new MorphoJFormat
        {
            scale = "",
            items = new List<MorphoJItem>()
        };

        private string ConvertedText = "";

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

        private void Converter_Click(object sender, RoutedEventArgs e)
        {
            ParseTextToTPSComplex();
            ParseTPS2MorphoJ();
            ConvertedText = MorphoJToString();
            ConvertedTPS.Text = ConvertedText;
        }

        private void SaveConversion_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "TPS files (*.tps)|*.tps|All files (*.*)|*.*";
            saveDialog.FilterIndex = 0;
            saveDialog.RestoreDirectory = true;
            if (saveDialog.ShowDialog() == true)
            {
                if (saveDialog.FileName != "")
                {
                    // FileStream fs = (FileStream) saveDialog.OpenFile();
                    File.AppendAllText(saveDialog.FileName, ConvertedText);
                    //                    fs.Close();
                }
            }
        }

        private void ParseTextToTPSComplex()
        {
            var allLines = OriginalTPS.Text.Split('\n');
            var scale = allLines.FirstOrDefault(l => l.StartsWith("SCALE"));
            if (scale != null)
            {
                parsed.scale = scale.Split('=')[1];
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
                        imageName = "",
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
                                imageItem.imageName = line.Split('=')[1];
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
        }

        private void ParseTPS2MorphoJ()
        {
            var idCounter = 0;
            output.scale = parsed.scale;
            parsed.images.ForEach(image =>
            {
                var lmArray = image.lms.OrderBy(lm => lm.index).ToArray();
                var curvesArray = image.curves.OrderBy(curve => curve.index).ToArray();

                string firstLM;
                string secondLM;
                var indexCounter = 0;
                var curveIndex = 0;
                for (var i = 0; i < image.lmCount; i = i + 2)
                {
                    // TODO: Check in the case where lmCount > curveCount
                    firstLM = lmArray[i].content;
                    secondLM = lmArray[i + 1].content;
                    if (curveIndex >= curvesArray.Length)
                    {
                        break;
                    }
                    while (curvesArray[curveIndex] == null || (curvesArray[curveIndex].pointsCount < image.curvesCount / 2))
                    {
                        curveIndex++;
                        if (curveIndex >= curvesArray.Length)
                        {
                            break;
                        }
                    }
                    if (curveIndex >= curvesArray.Length)
                    {
                        break;
                    }
                    var firstCurveIndex = curveIndex;
                    curveIndex++;

                    if (curveIndex >= curvesArray.Length)
                    {
                        break;
                    }
                    while (curvesArray[curveIndex] == null || (curvesArray[curveIndex].pointsCount < image.curvesCount / 2))
                    {
                        curveIndex++;
                        if (curveIndex >= curvesArray.Length)
                        {
                            break;
                        }
                    }
                    if (curveIndex >= curvesArray.Length)
                    {
                        break;
                    }
                    var secondCurveIndex = curveIndex;
                    curveIndex++;

                    var firstPoints = curvesArray[firstCurveIndex].pointItems.OrderBy(point => point.index).ToList();
                    firstPoints.Remove(firstPoints.First());
                    firstPoints.Remove(firstPoints.Last());
                    var secondPoints = curvesArray[secondCurveIndex].pointItems.OrderBy(point => point.index).ToList();
                    secondPoints.Remove(secondPoints.First());
                    secondPoints.Remove(secondPoints.Last());

                    var lmIndex = 0;

                    var lms = new List<LM>();
                    lms.Add(new LM
                    {
                        index = lmIndex,
                        content = firstLM
                    });
                    var firstLmPoints = firstPoints.Select(point =>
                    {
                        lmIndex++;
                        return new LM
                        {
                            index = lmIndex,
                            content = point.content
                        };
                    }).ToList();
                    lms.AddRange(firstLmPoints);

                    lmIndex++;
                    lms.Add(new LM
                    {
                        index = lmIndex,
                        content = secondLM
                    });
                    var secondLmPoints = secondPoints.Select(point =>
                    {
                        lmIndex++;
                        return new LM
                        {
                            index = lmIndex,
                            content = point.content
                        };
                    }).ToList();
                    lms.AddRange(secondLmPoints);

                    var item = new MorphoJItem
                    {
                        lmCount = lms.Count,
                        lms = lms.ToList(),
                        imageName = image.imageName,
                        imageIndex = indexCounter,
                        ID = idCounter
                    };
                    indexCounter++;
                    idCounter++;

                    output.items.Add(item);
                }
            });
        }

        private string MorphoJToString()
        {
            var lineBreak = Environment.NewLine;
            var finalString = "";
            output.items.OrderBy(i => i.ID).ForEach(i => {
                finalString += "LM=" + i.lmCount + lineBreak;
                i.lms.OrderBy(lm => lm.index).ForEach(lm => {
                    finalString += lm.content + lineBreak;
                });
                var image = i.imageName.Split('.');
                finalString += "IMAGE=" + image[0] + "_FL" + i.imageIndex + "." + image[1] + lineBreak;
                finalString += "@ID=" + i.ID + lineBreak;
            });
            finalString += "SCALE=" + output.scale + lineBreak;
            return finalString;
        }
    }

    /// MorphoJ text format
    /// LM=x1
    /// ...
    /// IMAGE=str1
    /// @ID=y1
    /// LM=x2
    /// ...
    /// IMAGE=str2
    /// @ID=y2
    /// SCALE=z

    public class MorphoJFormat
    {
        public List<MorphoJItem> items;
        public string scale;
    }

    public class MorphoJItem
    {
        public int lmCount;
        public List<LM> lms;
        public string imageName;
        public int imageIndex;
        public int ID;
    }

    /// TPS text format
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
        public string scale;
    }

    public class TPSImage
    {
        public int lmCount;
        public List<LM> lms;
        public int curvesCount;
        public List<PointSet> curves;
        public string imageName;
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
