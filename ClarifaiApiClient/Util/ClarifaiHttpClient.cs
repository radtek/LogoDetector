﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ClarifaiApiClient.Util
{
    using Models;
    public class ClarifaiHttpClient
    {
        public static bool ValidateServerCertificate(
                                                    object sender,
                                                    X509Certificate certificate,
                                                    X509Chain chain,
                                                    SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static string _apiEndPoint = "https://api.clarifai.com";
        private static string _tokenPath = "/v1/token/";
        private static string _predictPath_template = "/v2/models/%modelname%/outputs";//logo_detector_120
        private static string _trainmodelpath_template = "/v2/models/%modelname%/versions";

        private static string _predictPath = "/v2/models/%modelname%/outputs";//logo_detector_120
        private static string _trainmodelpath = "/v2/models/%modelname%/versions";

        private static string _trainPath = "/v2/inputs";
        public static void SetupModelPath(string ModelName)
        {
            _predictPath = _predictPath_template.Replace("%modelname%", ModelName);
            _trainmodelpath = _trainmodelpath_template.Replace("%modelname%", ModelName);
        }
        public static async Task<object> GetToken(string clientId, string clientSecret)
        {
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);

            //
            //  MESSAGE CONTENT
            string postData = "client_id="+ clientId + "&client_secret="+ clientSecret + "&grant_type=client_credentials";
            // string postData = "client_id=JU5zeSv_YJQG5THfAHwUvuD_oDFI13PqFKTXjKoS&client_secret=GQynbkt8D5mQcXUlZhcPqw7SAyQbTjj3WC1SYpkM&grant_type=client_credentials";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            
            //
            //  CREATE REQUEST
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _tokenPath);
            Request.Method = "POST";
            Request.KeepAlive = false;
            Request.ContentType = "application/x-www-form-urlencoded";
            Request.Headers.Add("cache-control", "no-cache");

            Stream dataStream = await Request.GetRequestStreamAsync();
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            dataStream.Close();

            //
            //  SEND MESSAGE
            try
            {
                WebResponse Response = await Request.GetResponseAsync();

                StreamReader Reader = new StreamReader(Response.GetResponseStream());
                string responseLine = await Reader.ReadToEndAsync();
                Reader.Close();

                HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
                if (!ResponseCode.Equals(HttpStatusCode.OK))
                {
                    TokenError error = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenError>(responseLine);
                    return error;
                }

                Token token = Newtonsoft.Json.JsonConvert.DeserializeObject<Token>(responseLine);
                return token;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        TokenError error = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenError>(text);
                        return error;
                    }
                }
            }
            
            return new TokenError
            {
                Status_Code = "Undefined Error",
                Status_Msg = "Undefined Error"
            };
        }

        public static async Task<Predict> GetImgUrlPrediction(string accessToken, List<string> imgURLs)
        {
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);

            //
            //  MESSAGE CONTENT
            //string postData = "client_id="+ clientId + "&client_secret="+ clientSecret + "&grant_type=client_credentials";
            List <PredictInput> inputs = new List<PredictInput>();
            foreach (string imgUrl in imgURLs) {
                inputs.Add(new PredictInput
                {
                    Data = new PredictImage
                    {
                        Image = new PredictImageData
                        {
                            Url = imgUrl
                        }
                    }
                });
            }
            var ins = new
            {
                Inputs = inputs
            };

            string postData = LowercaseJsonSerializer.SerializeObject(ins);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            //
            //  CREATE REQUEST
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _predictPath);
            Request.Method = "POST";
            Request.KeepAlive = false;
            Request.ContentType = "application/json";
            Request.Headers.Add("cache-control", "no-cache");
            Request.Headers.Add("authorization", "Bearer "+ accessToken);

            Stream dataStream = await Request.GetRequestStreamAsync();
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            dataStream.Close();

            //
            //  SEND MESSAGE
            try
            {
                WebResponse Response = await Request.GetResponseAsync();

                StreamReader Reader = new StreamReader(Response.GetResponseStream());
                string responseLine = await Reader.ReadToEndAsync();
                Reader.Close();

                HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
                if (!ResponseCode.Equals(HttpStatusCode.OK))
                {
                    Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                    return error;
                }

                Predict predict = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                int ind = 0;
                foreach (string imgUrl in imgURLs)
                {
                    var outputs = predict.Outputs;
                    outputs[ind].Data.Concepts[0].ImageName = imgUrl;
                    ind++;
                }
                return predict;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(text);
                        return error;
                    }
                }
            }

            return new Predict
            {
                Status = new PredictStatus
                {
                    Code = 0,
                    Description = "Undefined Error"
                }
            };
        }

        public static async Task<Predict> GetFolderImgsPrediction(string accessToken, string folder_path)
        {
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);
            try
            {


                //
                //  MESSAGE CONTENT
                //string postData = "client_id="+ clientId + "&client_secret="+ clientSecret + "&grant_type=client_credentials";
                List<PredictInput> inputs = new List<PredictInput>();
            var imgPaths = Directory.GetFiles(folder_path, "*.jpg", SearchOption.AllDirectories).ToList();
            foreach (string imgPath in imgPaths)
            {
                Bitmap source = new Bitmap(imgPath);
                int x = source.Width - 120;
                int y = source.Height - 120;
                Bitmap CroppedImage = source.Clone(new Rectangle(x, y, 120, 120), source.PixelFormat);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                CroppedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] imageArray = ms.ToArray();
                //byte[] imageArray = System.IO.File.ReadAllBytes(imgPath);
                string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                inputs.Add(new PredictInput
                {
                    Data = new PredictImage
                    {
                        Image = new PredictImageData
                        {
                            Base64 = base64ImageRepresentation
                        }
                    }
                });
            }
            var ins = new
            {
                Inputs = inputs
            };

            string postData = LowercaseJsonSerializer.SerializeObject(ins);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            //
            //  CREATE REQUEST
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _predictPath);
            Request.Method = "POST";
            Request.KeepAlive = false;
            Request.ContentType = "application/json";
            Request.Headers.Add("cache-control", "no-cache");
            Request.Headers.Add("authorization", "Bearer " + accessToken);

            Stream dataStream = await Request.GetRequestStreamAsync();
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            dataStream.Close();

           
            //
            //  SEND MESSAGE
           
                WebResponse Response = await Request.GetResponseAsync();

                StreamReader Reader = new StreamReader(Response.GetResponseStream());
                string responseLine = await Reader.ReadToEndAsync();
                Reader.Close();

                HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
                if (!ResponseCode.Equals(HttpStatusCode.OK))
                {
                    Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                    return error;
                }

                Predict predict = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                int ind = 0;
                foreach (string imgPath in imgPaths)
                {
                    var outputs = predict.Outputs;
                    outputs[ind].Data.Concepts[0].ImageName = imgPath;
                    ind++;
                }
                    return predict;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(text);
                        return error;
                    }
                }
            }catch(Exception er)
            {

            }

            return new Predict
            {
                Status = new PredictStatus
                {
                    Code = 0,
                    Description = "Undefined Error"
                }
            };
        }

        public static async Task<Predict> GetImgsPrediction(string accessToken, Dictionary<string, Bitmap> Images)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("Invalid or empty accessToken!");
            if (Images == null || Images.Count < 1)
                throw new Exception("list has no images to checkout!");

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);

                //
                //  MESSAGE CONTENT
                //string postData = "client_id="+ clientId + "&client_secret="+ clientSecret + "&grant_type=client_credentials";
                List<PredictInput> inputs = new List<PredictInput>();

                foreach (Bitmap source in Images.Values)
                {

                    MemoryStream ms = new System.IO.MemoryStream();
                    source.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] imageArray = ms.ToArray();

                    string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                    inputs.Add(new PredictInput
                    {
                        Data = new PredictImage
                        {
                            Image = new PredictImageData
                            {
                                Base64 = base64ImageRepresentation
                            }
                        }
                    });
                }
                var ins = new
                {
                    Inputs = inputs
                };

                string postData = LowercaseJsonSerializer.SerializeObject(ins);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                //
                //  CREATE REQUEST
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _predictPath);
                Request.Method = "POST";
                Request.KeepAlive = false;
                Request.ContentType = "application/json";
                Request.Headers.Add("cache-control", "no-cache");
                Request.Headers.Add("authorization", "Bearer " + accessToken);

                Stream dataStream = await Request.GetRequestStreamAsync();
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                dataStream.Close();



                WebResponse Response = await Request.GetResponseAsync();

                StreamReader Reader = new StreamReader(Response.GetResponseStream());
                string responseLine = await Reader.ReadToEndAsync();
                Reader.Close();

                HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
                if (!ResponseCode.Equals(HttpStatusCode.OK))
                {
                    Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                    return error;
                }

                Predict predict = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                int ind = 0;
                foreach (string imgPath in Images.Keys)
                {
                   
                    var outputs = predict.Outputs;
                    outputs[ind].Data.Concepts[0].ImageName = imgPath;
                    ind++;
                }
                return predict;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(text);
                        return error;
                    }
                }
            }
            catch (Exception er)
            {
                Predict error = new Predict { Status = new PredictStatus { Code = 0, Description = er.Message } };
                return error;
            }

          
        }

        public static async Task<PredictOutput> TrainWithImage(string accessToken,string ConceptID,  Bitmap Image,bool HasLogo)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("Invalid or empty accessToken!");
            if (Image == null)
                throw new ArgumentNullException("Image");

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);

                //
                //  MESSAGE CONTENT
                //string postData = "client_id="+ clientId + "&client_secret="+ clientSecret + "&grant_type=client_credentials";
                List<PredictInput> inputs = new List<PredictInput>();

               

                    MemoryStream ms = new System.IO.MemoryStream();
                    Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] imageArray = ms.ToArray();

                    string base64ImageRepresentation = Convert.ToBase64String(imageArray);


                string json_input = "";
                var concept = new ConceptData
                { 
                    Id = ConceptID,
                    Value = HasLogo ? "true" : "false"
                };
                var Concepts = new List<ConceptData>();
                Concepts.Add(concept);
                    inputs.Add(new PredictInput
                    { 
                        Data = new PredictImage
                        {
                            Image = new PredictImageData
                            {
                                Base64 = base64ImageRepresentation
                            },
                            Concepts = Concepts
                        }
                    });
                
                var ins = new
                {
                    Inputs = inputs
                };

                string postData = LowercaseJsonSerializer.SerializeObject(ins);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                //
                //  CREATE REQUEST
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _trainPath);
                Request.Method = "POST";
                Request.KeepAlive = false;
                Request.ContentType = "application/json";
                Request.Headers.Add("cache-control", "no-cache");
                Request.Headers.Add("authorization", "Bearer " + accessToken);

               
                Stream dataStream = await Request.GetRequestStreamAsync();
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                dataStream.Close();



                WebResponse Response = await Request.GetResponseAsync();

                StreamReader Reader = new StreamReader(Response.GetResponseStream());
                string responseLine = await Reader.ReadToEndAsync();
                Reader.Close();

                HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
                if (!ResponseCode.Equals(HttpStatusCode.OK))
                {
                    PredictOutput error = Newtonsoft.Json.JsonConvert.DeserializeObject<PredictOutput>(responseLine);
                    return error;
                }

                PredictOutput predict = Newtonsoft.Json.JsonConvert.DeserializeObject<PredictOutput>(responseLine);
               
               
                    var outputs = predict.Data;
                   
                
                
                return predict;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        var error = Newtonsoft.Json.JsonConvert.DeserializeObject<PredictOutput>(text);
                        return error;
                    }
                }
            }
            catch (Exception er)
            {
                var error = new PredictOutput {  Status = new PredictStatus { Code = 0, Description = er.Message } };
                return error;
            }


        }

        public static async Task<PredictOutput> TrainWithImages(string accessToken,string ConceptID, List<Bitmap> Images, bool HasLogo)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("Invalid or empty accessToken!");
            if (Images == null || Images.Count <1)
                throw new ArgumentNullException("Image");

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);

                //
                //  MESSAGE CONTENT
                //string postData = "client_id="+ clientId + "&client_secret="+ clientSecret + "&grant_type=client_credentials";
                List<PredictInput> inputs = new List<PredictInput>();


                foreach (var Image in Images)
                {


                    MemoryStream ms = new System.IO.MemoryStream();
                    Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] imageArray = ms.ToArray();

                    string base64ImageRepresentation = Convert.ToBase64String(imageArray);


                    var concept = new ConceptData
                    {
                        Id = ConceptID,
                        Value = HasLogo ? "true" : "false"
                    };
                    var Concepts = new List<ConceptData>();
                    Concepts.Add(concept);
                    inputs.Add(new PredictInput
                    {
                        Data = new PredictImage
                        {
                            Image = new PredictImageData
                            {
                                Base64 = base64ImageRepresentation
                            },
                            Concepts = Concepts
                        }
                    });
                }
                var ins = new
                {
                    Inputs = inputs
                };

                string postData = LowercaseJsonSerializer.SerializeObject(ins);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                //
                //  CREATE REQUEST
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _trainPath);
                Request.Method = "POST";
                Request.KeepAlive = false;
                Request.ContentType = "application/json";
                Request.Headers.Add("cache-control", "no-cache");
                Request.Headers.Add("authorization", "Bearer " + accessToken);


                Stream dataStream = await Request.GetRequestStreamAsync();
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
                dataStream.Close();



                WebResponse Response = await Request.GetResponseAsync();

                StreamReader Reader = new StreamReader(Response.GetResponseStream());
                string responseLine = await Reader.ReadToEndAsync();
                Reader.Close();

                HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
                if (!ResponseCode.Equals(HttpStatusCode.OK))
                {
                    PredictOutput error = Newtonsoft.Json.JsonConvert.DeserializeObject<PredictOutput>(responseLine);
                    return error;
                }

                PredictOutput predict = Newtonsoft.Json.JsonConvert.DeserializeObject<PredictOutput>(responseLine);


                var outputs = predict.Data;



                return predict;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        var error = Newtonsoft.Json.JsonConvert.DeserializeObject<PredictOutput>(text);
                        return error;
                    }
                }
            }
            catch (Exception er)
            {
                var error = new PredictOutput { Status = new PredictStatus { Code = 0, Description = er.Message } };
                return error;
            }


        }

        public static async Task<Predict> TrainModel(string accessToken)
        {
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);


            //
            //  CREATE REQUEST
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_apiEndPoint + _trainmodelpath);
            Request.Method = "POST";
            Request.KeepAlive = false;
            Request.ContentType = "application/json";
            Request.Headers.Add("cache-control", "no-cache");
            Request.Headers.Add("authorization", "Bearer " + accessToken);

            Stream dataStream = await Request.GetRequestStreamAsync();

            dataStream.Close();


            WebResponse Response = await Request.GetResponseAsync();

            StreamReader Reader = new StreamReader(Response.GetResponseStream());
            string responseLine = await Reader.ReadToEndAsync();
            Reader.Close();

            HttpStatusCode ResponseCode = ((HttpWebResponse)Response).StatusCode;
            if (!ResponseCode.Equals(HttpStatusCode.OK))
            {
                Predict error = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
                return error;
            }

            Predict predict = Newtonsoft.Json.JsonConvert.DeserializeObject<Predict>(responseLine);
          

            return predict;



        }

    }
}
