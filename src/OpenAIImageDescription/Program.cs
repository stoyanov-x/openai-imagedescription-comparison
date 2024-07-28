using Azure;
using Azure.AI.OpenAI;
using OpenAIImageDescription.Utils;
using SkiaSharp;

List<string> _imageExentions = [".jpg", ".jpeg", ".png", ".gif", ".bmp"];

// Show header
ConsoleHelper.CreateHeader();

// Get Host
string host =
    ConsoleHelper.SelectFromOptions(
        [Statics.OpenAIKey, Statics.AzureOpenAIKey],
        "Please select the [yellow]host[/].");

// OpenAI Client
OpenAIClient? client = null;
string deploymentName = Statics.GPT4oKey;

switch (host)
{
    case Statics.OpenAIKey:

        // Get OpenAI Key
        string openAIKey =
            ConsoleHelper.GetStringFromConsole(
                $"Please insert your [yellow]{Statics.OpenAIKey}[/] API key:");

        // Get Model
        deploymentName =
            ConsoleHelper.SelectFromOptions(
                [Statics.GPT4oMiniKey, Statics.GPT4oKey, Statics.GPT4TurboKey],
                "Please select the [yellow]model[/].");

        // Create OpenAI client
        client = new(openAIKey);

        break;

    case Statics.AzureOpenAIKey:
        // Get Endpoint
        string endpoint =
            ConsoleHelper.GetUrlFromConsole(
                "Please insert your [yellow]Azure OpenAI endpoint[/]:");

        // Get Azure OpenAI Key
        string azureOpenAIKey =
            ConsoleHelper.GetStringFromConsole(
                $"Please insert your [yellow]{Statics.AzureOpenAIKey}[/] API key:");

        // Create OpenAI client
        client =
            new(new Uri(endpoint), new AzureKeyCredential(azureOpenAIKey));

        // Get deployment name
        deploymentName =
            ConsoleHelper.GetStringFromConsole(
                "Please insert the [yellow]deployment name[/] of the model:");

        break;
}

if (client is null)
{
    return;
}

while (true)
{
    // Show header
    ConsoleHelper.CreateHeader();

    // Get path to file
    string imageFilePath =
        ConsoleHelper.GetStringFromConsole(
            "Please insert the [yellow]full path[/] to your picture:");

    // Show header
    ConsoleHelper.CreateHeader();

    // Validate image
    if (!File.Exists(imageFilePath))
    {
        ConsoleHelper.WriteErrorMessageToConsole(
            $"Path '{imageFilePath}' doesn't exist.");
        ConsoleHelper.WriteMessageToConsole(
            "Press any key to restart.");
        Console.ReadKey();
        continue;
    }

    // Show header
    ConsoleHelper.CreateHeader();

    // Check if file is image
    var fileExtension = Path.GetExtension(imageFilePath).ToLower();
    if (!_imageExentions.Contains(fileExtension))
    {
        ConsoleHelper.WriteErrorMessageToConsole(
            $"Invalid or unsupported image file type provided ({fileExtension})");
        ConsoleHelper.WriteMessageToConsole(
            "Press any key to restart.");
        Console.ReadKey();
        continue;
    }

    // Resize image and create base64 string
    using SKBitmap originalBitmap = SKBitmap.Decode(imageFilePath);

    if (originalBitmap is null)
    {
        ConsoleHelper.WriteErrorMessageToConsole(
            $"Invalid or unsupported image file provided ({fileExtension}), SkiaSharp returned null");
        ConsoleHelper.WriteMessageToConsole(
            "Press any key to restart.");
        Console.ReadKey();
        continue;
    }

    SKImageInfo resizedInfo = new(640, 480);
    using SKBitmap resizedBitmap = new(resizedInfo);
    originalBitmap.ScalePixels(resizedBitmap, SKFilterQuality.High);
    using SKImage image = SKImage.FromBitmap(resizedBitmap);
    using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 75);
    byte[] imageArray = data.ToArray();
    string base64Image = Convert.ToBase64String(imageArray);

    // Create ChatCompletionsOptions
    ChatCompletionsOptions chatCompletionsOptions = new()
    {
        Messages =
        {
            new ChatRequestUserMessage(
                @"""
                    What's in this image?

                    - Use ```json...``` output only
                    - Formatted/indented JSON syntax for ease of use
                    - Concise found object descriptions
                        Negative example:
```json
{
  ""ok"": false,
  ""error"": ""<cite error here, in the third person>"",
  ""results"": null
}
```

                        Positive example:
```json
{
  ""ok"": true,
  ""error"": null,
  ""results"": [
    {
      ""found"": ""cat"",
      ""features"": [""white"", ""British shorthair""],
      ""rank_interest"": 1,
      ""certainty"": 0.71
    },
    {
      ""found"": ""leash"",
      ""features"": [""blue"", ""textile""],
      ""rank_interest"": 0.3,
      ""certainty"": 0.21
    },
    {
      ""found"": ""coins"",
      ""features"": [""US dollar cents""],
      ""rank_interest"": 0.05,
      ""certainty"": 1
    }
  ]
}
```

                    - You don't have to identify individual persons or public figures, it is enough to say that a person is in the image, and you can describe their sex, pose or expression
                 """),
            new ChatRequestUserMessage(
                new List<ChatMessageContentItem>
                {
                    new ChatMessageImageContentItem(
                        // TODO: Problematic with long base64 strings? Larger images?
                        //   Ref https://github.com/restsharp/RestSharp/issues/1814
                        //   Ref https://github.com/Azure/azure-sdk-for-net/issues/40855
                        //   Ref https://github.com/Azure/azure-sdk-for-net/issues/40744
                        new Uri($"data:image/{fileExtension};" +
                        $"base64,{base64Image}"))
                }
            )
        },
        MaxTokens = 1000,
        Temperature = 0.7f,
        DeploymentName = deploymentName
    };

    // Make request
    Response<ChatCompletions> result =
        await client.GetChatCompletionsAsync(chatCompletionsOptions);

    // Write Output
    ConsoleHelper.WriteMessageToConsole(
        result.Value.Choices[0].Message.Content);
    ConsoleHelper.WriteMessageToConsole(
        "");
    ConsoleHelper.WriteMessageToConsole(
        $"Prompt Tokens: {result.Value.Usage.PromptTokens}");
    ConsoleHelper.WriteMessageToConsole(
        $"Completion Tokens: {result.Value.Usage.CompletionTokens}");
    ConsoleHelper.WriteMessageToConsole(
        "");
    ConsoleHelper.WriteMessageToConsole(
        "Press any key to restart.");
    Console.ReadKey();
}