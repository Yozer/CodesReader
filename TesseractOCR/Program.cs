using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace TesseractOCR
{
    class Program
    {
        // 464/19516
        static void Main(string[] args)
        {
            int hit, all;
            hit = all = 0;
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                foreach (var file in Directory.EnumerateFiles(@"D:\dataset\easy\first try\read_and_segmented"))
                {
                    string code = Path.GetFileNameWithoutExtension(file);
                    engine.DefaultPageSegMode = PageSegMode.SingleLine;
                    engine.SetVariable("load_system_dawg", "false");
                    engine.SetVariable("load_freq_dawg", "false");
                    engine.SetVariable("tessedit_char_whitelist", "-2346789BCDFGHJKMPQRTVWXY");
                    using (var img = Pix.LoadFromFile(file))
                    using (var page = engine.Process(img))
                    {
                        var text = page.GetText().Trim();
                        if (text == code)
                        {
                            ++hit;
                        }
                    }


                    ++all;
                }
            }
            Console.WriteLine($"{hit}/{all}");
            Console.ReadKey();
        }
    }
}
