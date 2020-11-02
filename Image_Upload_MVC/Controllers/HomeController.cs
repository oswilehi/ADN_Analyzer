using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace Image_Upload_MVC.Controllers
{
    public class HomeController : Controller
    {

        IFaceClient client = new FaceClient(new ApiKeyServiceClientCredentials("83f082ff92f64df9adf46ccf3febcdc0")) { Endpoint = "https://proyectos.cognitiveservices.azure.com/face/v1.0/detect" };
        string message;
        

        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(HttpPostedFileBase[] postedFile)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if (postedFile != null)
            {
                string firstPhoto = Path.GetFileName(postedFile[0].FileName);
                string secondPhoto = Path.GetFileName(postedFile[1].FileName);
                string path = Server.MapPath("~/Uploads/");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                postedFile[0].SaveAs(path + firstPhoto);
                ViewBag.FirstImage = "Uploads/" + firstPhoto;
                postedFile[1].SaveAs(path + secondPhoto);
                ViewBag.SecondImage = "Uploads/" + secondPhoto;

            }

            await Analyze();

            return View();
        }

        [HttpPost]
        public ActionResult SendEmail()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(Server.MapPath("~/Uploads/"));
            var files = di.GetFiles();

            string EmailEmisor = "adpdsg4@gmail.com";
            string EmailReceptor = Request.Form["email"].Trim();
            string contraseña = "12345678grupo4";


            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(EmailEmisor);
                mail.To.Add(EmailReceptor);
                mail.Subject = "ANALISIS ADN";
                mail.Body = "<h1>RESULTADO</h1> <br /> <h3> " + Request.Form["content"].Trim() + " </h3>";
                mail.IsBodyHtml = true;
                mail.Attachments.Add(new Attachment(files[0].FullName));
                mail.Attachments.Add(new Attachment(files[1].FullName));



                using (SmtpClient correo = new SmtpClient("smtp.gmail.com", 587))
                {
                    correo.Credentials = new System.Net.NetworkCredential(EmailEmisor, contraseña);
                    correo.EnableSsl = true;
                    correo.Send(mail);
                    Console.WriteLine("Se envio tu correo");
                }
            }

            //Delete photos
      
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            return View("Email");
        }

        public ActionResult GoToIndex()
        {
            return View("Index");
        }

        public async Task Analyze()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            // Analyze individual images
            var files = Directory.GetFiles(Server.MapPath("~/Uploads/"));
            string face1 = await MakeAnalysisRequest(files[0].ToString());
            string face2 = await MakeAnalysisRequest(files[1].ToString());

            JArray jsonArray0 = JArray.Parse(face1);
            JArray jsonArray1 = JArray.Parse(face2);

            dynamic data = JObject.Parse(jsonArray0[0].ToString());
            dynamic data1 = JObject.Parse(jsonArray1[0].ToString());

            //Get face id of images
            string faceID = data.faceId;
            string faceID1 = data1.faceId;


            //Obtener porcentaje de parentesco
            dynamic kinship = JObject.Parse(await SimilarFaces(faceID, faceID1));


            string percentage = kinship.confidence;
            message = GetKinship(percentage) + " con un porcentaje de acierto del " + Math.Round(float.Parse(percentage), 2) * 100 + "% ";
            ViewBag.Kinship = message;

           
        }

        static async Task<string> MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", "83f082ff92f64df9adf46ccf3febcdc0");

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = "https://proyectos.cognitiveservices.azure.com/face/v1.0/detect" + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();
                return contentString;
            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        static async Task<string> SimilarFaces(string faceID1, string faceID2)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", "83f082ff92f64df9adf46ccf3febcdc0");

            // Assemble the URI for the REST API Call.
            string uri = "https://proyectos.cognitiveservices.azure.com/face/v1.0/verify";

            string body = "{\"faceId1\": " + "\"" + faceID1 + "\" ," + "\"faceId2\": " + "\"" + faceID2 + "\"}";

            HttpResponseMessage response = await client.PostAsync(uri, new StringContent(body, Encoding.UTF8, "application/json"));

            // Get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();

            return contentString;

        }


        static string GetKinship(string confidence)
        {
            float percentage = float.Parse(confidence);

            switch (percentage)
            {
                case float n when (percentage <= 0.20):
                    return "No existe afinidad";
                case float n when (percentage > 0.20 && percentage <= 0.40):
                    return "Primos lejanos";
                case float n when (percentage > 0.40 && percentage <= 0.60):
                    return "Primos o tios";
                case float n when (percentage > 0.60 && percentage <= 0.80):
                    return "Hermanos";
                case float n when (percentage > 0.80 && percentage <= 0.90):
                    return "Papa/Mama";
                case float n when (percentage > 0.90 && percentage <= 1):
                    return "La misma persona";
                default:
                    break;
            }

            return "";
        }

    }
}