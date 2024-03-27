using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
//using Nest;
using Elastic.Transport;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using static System.Net.Mime.MediaTypeNames;
using Elastic.Apm.Api;
using System.ComponentModel;
using Elasticsearch.Net;
using Elastic.Clients;
using FluentAssertions;
using Nest;
using Elastic.Transport;


using ConnectionConfiguration = Elastic.Clients.Elasticsearch.ConnectionConfiguration;
using Aspose.Foundation.UriResolver.RequestResponses;
using System.Xml.Linq;
using StringResponse = Elasticsearch.Net.StringResponse;
using Newtonsoft.Json;





namespace WebApplication16.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
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
                var fileName2 = $"{Guid.NewGuid()}.png";                //новый имя новой чб картинки
                var filePath2 = Path.Combine(uploadsFolder, fileName2); //новый путь новой чб картинки
                var SourceImage = Bitmap.FromFile(filePath);//создание картинки в формате Bitmap 
                NewImage(SourceImage, fileName2);//вызов функции создающей чб картинку
                var answerArray = GetImageText(filePath2);
                //Indexing(answerArray, answerArray.Count());
                return Ok(answerArray);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private void NewImage(System.Drawing.Image SourceImage, string fileName)
        {
            using Graphics gr = Graphics.FromImage(SourceImage);// SourceImage is a Bitmap object
            var gray_matrix = new float[][] {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0,      0,      0,      1, 0 },
                    new float[] { 0,      0,      0,      0, 1 }
                    };

            var ia = new ImageAttributes();
            ia.SetColorMatrix(new ColorMatrix(gray_matrix));

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

        
        [HttpGet("Responce1{name}")]
        public async Task<IActionResult> Indexing2(string name)
        {
            var settings = new ConnectionSettings(new Uri("https://177091d9acb14dd1a50694dd1c5fe0ea.eastus2.azure.elastic-cloud.com:443"))
                .BasicAuthentication("elastic", "CxfUwkDTrBbpfx5P50kXMwTL")
                .DefaultIndex("mybase")
                .DisableDirectStreaming();

            /*var comp = new object[]
            {
                new {index = new {_index="mybase", _id ="1"}},
                new {id = 1, Name = "water", Description = "Основной компонент, растворитель.Безопасен в использовании."},
                new {index = new {_index="mybase", _id ="2"}},
                new {id = 4, Name = "aqua", Description = "Основной компонент, растворитель.Безопасен в использовании."},
                new {index = new {_index="mybase", _id ="3"}},
                new {id = 5, Name = "glycerin", Description = "Влагоудерживающий компонент, растворитель, денатурат. Смягчающее, защитное, увлажняющее действие. Растворитель, регулятор вязкости, эмульгатор.Безопасен при использовании по назначению."},
            };*/
            var client = new ElasticLowLevelClient(settings);
            //индексирование       
            //var response = await client.BulkAsync<StringResponse>(Elasticsearch.Net.PostData.MultiJson(comp));

            //неточный поиск
            var searchResponce = await client.SearchAsync<StringResponse>("mybase", Elasticsearch.Net.PostData.Serializable(new
            {
                size = 100,
                from = 0,
                query = new
                {
                    match = new
                    {
                        Name = new
                        {
                            query = name,
                            fuzziness = "2"

                        }
                    }
                }
            })) ;
            
            string responsestr = searchResponce.Body;
            if (string.IsNullOrWhiteSpace(responsestr))
            {
                Console.WriteLine("Response string is empty or null.");
                return NotFound();
            }
            else
            {
                //десериализация ответа и получение нужной информации о найденном объекте
                var searchResponse = JsonConvert.DeserializeObject<HitsResponse>(responsestr);
                string answer = "";
                foreach (var hitItem in searchResponse.hits.hits)
                {
                    Console.WriteLine($"Name: {hitItem._source.name}, Description: {hitItem._source.description}");
                    answer += (hitItem._source.name + "\n" + hitItem._source.description);
                }
                return Ok(answer);
            }

        }




    }
}
