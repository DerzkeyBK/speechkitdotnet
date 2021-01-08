using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;

namespace speechkit
{
    //ru-RU
    class Program
    {
        const string postURL = "https://transcribe.api.cloud.yandex.net/speech/stt/v2/longRunningRecognize";
        const string getURL = "https://operation.api.cloud.yandex.net/operations/";
        static void Main(string[] args)
        {
            //https://storage.yandexcloud.net/dbktestbucket/lec2.ogg
            List<int> leclist = new List<int>() { 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14 };
            Dictionary<int,string> opDict = new Dictionary<int, string>();
            //var url = Console.ReadLine();
            foreach(var value in leclist)
            {

                #region POST-запрос
                RequestBody body = new RequestBody()
                {
                    config = new RequestConfig()
                    {
                        specification = new RequestSpecification()//конфиг для опуса,подробнее в документации апи
                        {
                            languageCode = "ru-RU",
                            profanityFilter = "false",
                            rawResults = "false",
                            audioEncoding = "OGG_OPUS",
                            sampleRateHertz = 48000,
                            model = "general"
                        }
                    },
                    audio = new RequestAudio()
                    {
                        uri = $""//ссылка на необходимый файл залитый в баскет на яндексе
                    }
                };

                var data = JsonSerializer.Serialize(body);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(postURL);
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", "Api-Key");//вставь api-key от yandex cloud
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.ContentLength = data.Length;
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }
                #endregion
                string result = "";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                var postResult = JsonSerializer.Deserialize<Operation>(result);
                opDict.Add(value,postResult.id.ToString());
            }

            string totalText = "";
            foreach(var value in leclist)
            {
                Console.Clear();
                var flag = true;
                var textResult = "";
                var start = DateTime.Now;
                string result = "";
                while (flag)
                {
                    var httpWebGet = (HttpWebRequest)WebRequest.Create(getURL +opDict[value]);
                    httpWebGet.Method = "GET";
                    httpWebGet.Headers.Add("Authorization", "Api-Key AQVNz4N1-jmnMVsz0ZO5J4YG24DrzMph-TvdN-oT");
                    var httpGetResponse = (HttpWebResponse)httpWebGet.GetResponse();
                    using (var streamReader = new StreamReader(httpGetResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                    }
                    var getJson = JsonSerializer.Deserialize<GetRequestBody>(result);
                    if (!getJson.done)
                    {
                        Console.WriteLine($"file not ready. time elapsed {(DateTime.Now - start).TotalSeconds}");
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        if (getJson.response == null)
                        {
                            Console.WriteLine($"Something went wrong see json response");
                            Console.WriteLine(result);
                            Console.ReadLine();

                        }
                        else
                        {
                            var ProcessedData = getJson.response.chunks.Select(x => x.alternatives.First().text).Distinct();
                            var count = 0;
                            var text = "";
                            while (count < ProcessedData.Count())
                            {
                                text += string.Join(". ", ProcessedData.Skip(count).Take(10)) + "\n";
                                count += 10;
                            }
                            Console.Clear();
                            Console.WriteLine($"file ready. time elapsed {(DateTime.Now - start).TotalSeconds}");
                            totalText+=$"Лекция номер {value}\n"+text;
                        }
                        flag = false;
                    }
                }
            }
            Console.WriteLine(totalText);
            Console.ReadLine();
        }
    }

    //модельки,было лень выносить в отдельный файл.
    public class RequestBody
    {
        public RequestConfig config { get; set; }
        public RequestAudio audio { get; set; }
    }

    public class RequestConfig
    {
        public RequestSpecification specification { get; set; }
    }

    public class RequestSpecification
    {
        public string languageCode { get; set; }
        public string model { get; set; }
        public string profanityFilter { get; set; }
        public string audioEncoding { get; set; }
        public int sampleRateHertz { get; set; }
        public string rawResults { get; set; }
    }

    public class RequestAudio
    {
        public string uri { get; set; }
    }

    public class Operation
    {
        public bool done { get; set; }
        public string id { get; set; }
        public string createdAt { get; set; }
        public string createdBy { get; set; }
        public string modifiedAt { get; set; }
    }

    public class GetRequestBody
    {
        public bool done { get; set; }

        public GetRequestResponse response { get; set; }

        public string id { get; set; }
        public string createdAt { get; set; }
        public string createdBy { get; set; }
        public string modifiedAt { get; set; }

    }
    
    public class GetRequestResponse
    {
        public string @type { get; set; }

        public List<Chunk> chunks { get; set; }

    }

    public class Chunk
    {
        public List<Alternative> alternatives { get; set; }

        public string channelTag { get; set; }
    }

    public class Alternative
    {
        public List<Word> words { get; set; }

        public string text { get; set; }

        public int confidence { get; set; }
    }

    public class Word
    {
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string word { get; set; }
        public int confidence { get; set; }
    }

}

