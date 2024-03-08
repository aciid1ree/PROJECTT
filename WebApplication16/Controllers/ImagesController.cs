using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Aspose.Pdf.Facades;
using Aspose.OCR;


namespace WebApplication16.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {

        public class MainText()
        {
            public string text { get; set; }

            public IEnumerable<string> GetArray()
            {

                IEnumerable<string> words = text.Split(new char[] {' ', ',', '!', '\n', '?', '\t'}, StringSplitOptions.RemoveEmptyEntries).Select(e => e.ToLower());

                return words;
            }

        }


        [HttpPost("Upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is not selected or empty");
            }

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var fileName = $"{Guid.NewGuid()}.png";
                var filePath = Path.Combine(uploadsFolder,fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);//копирование цветной картинки в проект
                }
                var fileName_2 = $"{Guid.NewGuid()}.png";                //новый имя новой чб картинки
                var filePath_2 = Path.Combine(uploadsFolder, fileName_2); //новый путь новой чб картинки
                var SourceImage = Bitmap.FromFile(filePath);//создание картинки в формате Bitmap 
                NewImage(SourceImage, fileName_2);//вызов функции создающей чб картинку
                var answerArray = GetImageText(filePath_2);
                return Ok(answerArray);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private void NewImage(Image SourceImage, string fileName)
        {
            using Graphics gr = Graphics.FromImage(SourceImage);// SourceImage is a Bitmap object
            var gray_matrix = new float[][] {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0,      0,      0,      1, 0 },
                    new float[] { 0,      0,      0,      0, 1 }
                    };

            var ia = new System.Drawing.Imaging.ImageAttributes();
            ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(gray_matrix));
            ia.SetThreshold((float)0.7); // Change this threshold as needed
            var rc = new Rectangle(0, 0, SourceImage.Width, SourceImage.Height);

            gr.DrawImage(SourceImage, rc, 0, 0, SourceImage.Width, SourceImage.Height, GraphicsUnit.Pixel, ia);
            SourceImage.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
        }

        

        private IEnumerable<string> GetImageText(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) {
                return Array.Empty<string>();
            }
            using var engine = new TesseractEngine(Path.Combine(Directory.GetCurrentDirectory(), "tessdata"), "rus+eng", EngineMode.Default);
           
            using Pix pix = Pix.LoadFromFile(imagePath);
            using Tesseract.Page page = engine.Process(pix);
            string text = page.GetText();
            MainText t1 = new MainText { text = text };
            return t1.GetArray();
        }

    }
}
