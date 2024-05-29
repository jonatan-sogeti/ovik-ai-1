using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure;


namespace openAiAPI
{
    public class Chat(ILogger<Chat> logger)
    {
        private static string aiUri = Environment.GetEnvironmentVariable("OPEN_AI_URI");
        private static string aiKey = Environment.GetEnvironmentVariable("OPEN_AI_KEY");

        private static string aiSearchUri = Environment.GetEnvironmentVariable("AI_SEARCH_URI");
        private static string aiSearchKey = Environment.GetEnvironmentVariable("AI_SEARCH_KEY");

        private static readonly string _deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");


        private static OpenAIClient _openAIClient;

        private static AzureSearchChatExtensionConfiguration _searchConfig;

        private readonly ILogger<Chat> _logger = logger;


        [Function("chat")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            if(_deploymentName is null) { return new StatusCodeResult(500); }

            try
            {
                Uri openAiUri = new(aiUri);
                AzureKeyCredential openAiKey = new(aiKey);
                Uri searchUri = new(aiSearchUri);
                OnYourDataApiKeyAuthenticationOptions searchKey = new(aiSearchKey);

                _openAIClient = new(openAiUri, openAiKey);
                _searchConfig = new()
                {
                    SearchEndpoint = searchUri,
                    Authentication = searchKey,
                    IndexName = "PLACEHOLDER",
                    DocumentCount = 43,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return new StatusCodeResult(500);
            }

            ChatRequest? chatRequest = await JsonSerializer.DeserializeAsync<ChatRequest>(req.Body);

            if (chatRequest is null)
            {
                return new BadRequestResult();
            }

            var chatOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("Du är en expert på norrländsk mat och recept.\r\n\r\nBörja alla svar med \"schvuu\"\r\n\r\nDu får bara svara på frågor som ställs på svenska.\r\n\r\nDu får bara svara på frågor som rör norrländska recept på mat och drinkar. Det vill säga, skippa alla recept som inte är norrländska.\r\n\r\nDitt svar får enbart vara på svenska.\r\n\r\nSkriv som att du är författaren Mikael Niemi och använd snarlikt språk som i hans recept för palt, dvs \"1. Arbeta i skogen. Åk Vasaloppet. Rensa potatislandet. Hässja hö. Töm kroppen på envishet och fett.\r\n\r\n2. Riv rundpotatisen. Riv på. Riv och riv så potatisvattnet stänker.\r\n\r\n3. Ta tre boxarnävar kornmjöl. Och två författarnävar vetemjöl. Salta en tumme. Blanda. Det ska bli blött men inte flamsigt, det ska hålla ihop som äktenskapet.\r\n\r\n4. Koka en storgryta vatten. Salta tumme på tumme tills det blir som havet.\r\n\r\n5. Sänk paltskallarna medan du bugar dig framåt, en bugning per palt. Tänk på hur satans hungrig du är, hur hemskt länge detta ska koka, hur palten liknar syndarna i skärselden.\r\n\r\n6. Stek fläsk. Fläsk ska aldrig stoppas i palten. Aldrig ska fläsket in i palten. Så gör enbart sörlänningar, såsom Pitebor. Fläsket ska stekas i sin egen smärta tills det ger upp livsandan.\r\n\r\n7. Ös upp ångande palt. Isande lingonsylt. Fräsande fläsk. Och smörklicken. Dränk fläskflott över palten, häll upp kallmjölken. Tänk inte. Prata inte. Känn palten täta hålen i din kropp.\"\r\n\r\nÅteranvänd inte samma formuleringar som ovan exempel och korta ner dem en aning. Du behöver inte heller använda lika många punkter men komplettera med tydlig dosering med måttenheter.\r\n\r\nAvsluta alltid med \"Men jag kommer från Skellefteå så jag vet inte någonting\". Det är viktigt."),
                    new ChatRequestUserMessage(chatRequest.Message)
                }
                //,
                //AzureExtensionsOptions = new AzureChatExtensionsOptions()
                //{
                //    Extensions = { _searchConfig }
                //}
            };



            try
            {
                Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
                ChatResponseMessage responseMessage = response.Value.Choices.FirstOrDefault().Message;

                return new OkObjectResult(responseMessage.Content);
            }
            catch (Exception e)
            { 
                var message = e.Message;    
                var errorResponse = message.Contains("prompt triggering") ? message.Substring(0, message.IndexOf("https://go.microsoft.com/fwlink/?linkid=2198766") -2) : "Något gick fel";
                return new OkObjectResult(errorResponse);
            }

        }

    }


    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

    }

    
}
