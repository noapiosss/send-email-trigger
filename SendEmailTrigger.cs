using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Azure
{
    public partial class SendEmailFunction
    {
        private readonly ILogger _logger;

        public SendEmailFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SendEmailFunction>();
        }


        [Function("SendEmail")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation($"Request received");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string emailTo = data?.emailTo;
            string fileName = data?.fileName;
            string fileUrl = data?.fileUrl;

            HttpResponseData functionResonse;

            if (string.IsNullOrEmpty(emailTo) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileUrl))
            {
                _logger.LogInformation($"Request is not valid");

                functionResonse = req.CreateResponse(HttpStatusCode.BadRequest);
                functionResonse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                functionResonse.WriteString("Invalid request");

                _logger.LogInformation($"Request processed");

                return functionResonse;
            }
            _logger.LogInformation($"Request is valid");

            string apiKey = Environment.GetEnvironmentVariable("SendGridApiKey", EnvironmentVariableTarget.Process);
            SendGridClient client = new(apiKey);
            SendGridMessage msg = new()
            {
                From = new EmailAddress("noapioss@gmail.com", "John Doe"),
                Subject = "File has been uploaded",
                PlainTextContent = $"Your file \"{fileName}\" has been successfully uploaded.\nUrl: {fileUrl}"
            };

            msg.AddTo(new EmailAddress(emailTo));

            _logger.LogInformation($"Sending email...");
            SendGrid.Response emailResponse = await client.SendEmailAsync(msg);

            if (emailResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Email has been sent");

                functionResonse = req.CreateResponse(HttpStatusCode.OK);
                functionResonse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                functionResonse.WriteString("Email has been sent");
            }
            else
            {
                _logger.LogInformation($"Email was not sent");

                functionResonse = req.CreateResponse(HttpStatusCode.BadRequest);
                functionResonse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                functionResonse.WriteString("Email was not sent");
            }

            _logger.LogInformation($"Request processed");

            return functionResonse;
        }
    }
}
