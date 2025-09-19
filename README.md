# Blackbird.Filters

Blackbird.Filters is a .NET library for processing and transforming content between different formats, with a focus on HTML content and XLIFF (XML Localization Interchange File Format) files. It provides tools for content extraction, transformation, and serialization.

## Main Library Structure

![1750852119651](image/readme/1750852119651.png)

## Classes and concepts

The library is organized around two main components:

### CodedContent

`CodedContent` is the core class for representing content extracted from a file:

- **Original**: The original file content as plain text
- **TextUnits**: A list of extracted text units
- **CreateTransformation**: Creates a transformation object from the coded content
- **Serialize**: Serializes the content using the content coder that matches its content type

### Transformation

`Transformation` represents a transformation of content from one language to another. Some methods and properties include:

- **GetUnits**: Gets all units recursively
- **Source**: Gets the source as coded content
- **Target**: Gets the target as coded content
- **Parse**: Static method to parse content from a string or stream. The string or stream can contain XLIFF or HTML content, both will be deserialized using the appropriate serializer. If no appropriate deserializer is found it will throw an exception
- **Serialize**: Serializes the transformation in the default way: using XLIFF 2.2

`Unit` the unit is the smallest indivisible unit of language that contains processing information like metadata, notes, quality scores, provenance, etc.
- **Segments**: the segments of this unit.
- **GetSource** and **GetTarget**: get the source and target as a single string. Useful when content only needs to be reviewed/analyzed. Loop through the segments instead if the target needs to be updated.

`Segment` is the subdivision of units that language processing tools can make. The target text can only be updated on this level due to this subdivision. Blackbird itself will never segment down from units and will thus produce one segment per unit. However, other tools can. See processing.

## Recommended use

When processing transformations, one can use and update the following:

- Use notes from `transformation.Notes`. Use these notes as extra context (f.e. for LLM operations), it can include style guides and other rules in human readable text.
- Loop through `transformation.GetUnits()` to get the classes processing information can be attached to. For batching see the code examples section.
- For translation/editing, loop through `unit.Segments` and process each segment individually. Optionally filter by the segment state and whether it is ignorable. Get the source with `segment.GetSource()` and use it to update the target with `segment.SetTarget()`.
    - When updating a segment. Update the `segment.State` to either `SegmentState.Translated` or `SegmentState.Reviewed`.
- For analysis/scoring, simply call `unit.GetSource()` and `unit.GetTarget()`.
    - Add scoring by setting the `unit.Quality.Score` or other fields on the `Quality` object.
- Add Provenance information to the `unit.Provenance.Translation` or `unit.Provenance.Review` object.
    - The `Person`, `Organization` and `Tool` field need human readable inputs (if known). The `PersonReference`, `OrganizationReference` and `ToolReference` fields ideally contain URLs so they can be displayed.

> Segments hold individual source/target pairs, but most metadata is stored on a unit level. Therefore it is recommended to loop through units (see example) and process these separately.

## Code examples

### Simple translation
```cs
    public string TranslateFile(string fileContent)
    {
        // File content can be either HTML or XLIFF (more formats to follow soon)
        var transformation = Transformation.Parse(fileContent);

        foreach (var unit in transformation.GetUnits())
        {
            foreach(var segment in unit.Segments.Where(x => !x.IsIgnorbale && x.IsInitial))
            {
                // Implement API calls here
                segment.SetTarget(segment.GetSource() + "TRANSLATED");

                // More state manipulations can be performed here
                segment.State = SegmentState.Translated;
            }

            // Unit level data should be updated here
            unit.Provenance.Translation.Tool = "Pseudo";
            unit.Provenance.Translation.ToolReference = "www.example.com/pseudo";
        }

        // To continue and pass it as a transformation
        return transformation.Serialize();

        // To get the target as the original content format:
        return transformation.Target().Serialize();
    }
```

### Batch translation (when an API can take multiple segments at once and returns them in order)

##### Synchronous (example from ModernMT)
```cs
    [Action("Translate", Description = "Translate file content retrieved from a CMS or file storage. The output can be used in compatible actions")]
    public async Task<FileTranslationResponse> TranslateFile([ActionParameter] TranslateFileRequest input)
    {
        // [...Some verification code here]

        var client = new ModernMtClient(Credentials);
        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content = await Transformation.Parse(stream);
        var translations = content
            .GetUnits() // Get all the units across files and groups
            .Batch(100, x => x => !x.IsIgnorbale && x.IsInitial) // Set an appropriate batch size depending on what the API can handle. Optionally (but recommended) filter for segments that actually need to be translated
            .Process(batch => client.Translate( // Apply the API translation method, takes a single batch
                input.SourceLanguage, 
                input.TargetLanguage, 
                batch.Select(x => x.Segment.GetSource()).ToList(), // You have acces to both x.Unit and x.Segment. Unit is only for reference if you need unit data during translation
                input.Hints?.Select(long.Parse).ToArray(), 
                input.Context, 
                input.CreateOptions())
            );


        var billedCharacters = 0;
        // Loop over each unit result. .Process() returns all processed units together with each specific (Segment, Result) pair for you to update the segments and units.
        foreach(var (unit, results) in translations)
        {
            foreach(var (segment, translation) in results)
            {
                segment.SetTarget(translation.TranslationText); // Update the target
                segment.State = Enums.SegmentState.Translated; // Update other variables
                billedCharacters += translation.BilledCharacters; // Update other counters relevant to the output depending on the app
            }

            // Update unit level information
            unit.Provenance.Translation.Tool = "ModernMT";
            unit.Provenance.Translation.ToolReference = "https://www.modernmt.com/";
        }
    }
```

##### Async example (DeepL)
```cs
    private async Task<FileResponse> HandleInteroperableTransformation(Transformation content, ContentTranslationRequest input)
    {
        // [...Some option setup code here]

        // You'll probably want to separate the translation method for readability and proper typing.
        async Task<IEnumerable<TextResult>> BatchTranslate(IEnumerable<(Unit Unit, Segment Segment)> batch)
        {
        return await ErrorHandler.ExecuteWithErrorHandlingAsync(async () =>
                    await Client.TranslateTextAsync(batch.Select(x => x.Segment.GetSource()), content.SourceLanguage, input.TargetLanguage, options));
        }

        // The rest should be the same
        var translations = await content.GetUnits().Batch(100, x => !x.IsIgnorbale && x.IsInitial).Process(BatchTranslate);

        var sourceLanguages = new List<string>();
        foreach(var (unit, results) in translations)
        {
            foreach(var (segment, result) in results)
            {
                segment.SetTarget(result.Text);
                segment.State = SegmentState.Translated;

                if (!string.IsNullOrEmpty(result.DetectedSourceLanguageCode))
                {
                    sourceLanguages.Add(result.DetectedSourceLanguageCode.ToLower());
                }
            }
            unit.Provenance.Translation.Tool = "DeepL";
            unit.Provenance.Translation.ToolReference = "https://www.deepl.com/"
        }
    }
```

## Serializers & Content coders:

### HtmlContentCoder

The `HtmlContentCoder` is responsible for processing HTML content:

- **Deserialize**: Converts HTML content into a structured `CodedContent` object
- **Serialize**: Converts a `CodedContent` object back to HTML
- **IsHtml**: Checks if a string is valid HTML content

The HTML coder handles:
- Inline elements vs. block elements
- Translatable attributes (alt, title, content, placeholder)
- Ignored elements (script, style)
- Whitespace normalization

### XliffSerializer

The `Xliff2Serializer` handles XLIFF 2.x format:

- **Deserialize**: Converts XLIFF content into a `Transformation` object
- **Serialize**: Converts a `Transformation` object back to XLIFF
- **IsXliff2**: Checks if a string is valid XLIFF 2.x content

The XLIFF serializer is compatible with the entire XLIFF 2.x standard. It supports additional custom XML tags. Not all submodules of XLIFF 2.x are *semantically supported* but their nodes will be persisted.
If you need an XLIFF 2.0 or 2.1 document you can use this serializer and specify the format as the second argument instead of calling `Transformation.Serialize()`.

## Testing
Every serializer and content coder has an extensive set of test cases. F.e. the XLIFF 2.x test cases came directly from the XLIFF TC repository.

Finally, you can use the test Heap to test production files. The folder Heap/Files is ignored, and test cases are automatically generated for each file in here (remember to set copy to output directoy for each file). You can use this folder to drop in production files that can contain sensitive information.

## Publishing to NuGet Manually

To publish the Blackbird.Filters library to NuGet manually:

1. **Build the package**:
   ```
   dotnet pack -c Release
   ```

2. **Generate the NuGet package**:
   This will create a `.nupkg` file in the `bin/Release` directory.

3. **Push to NuGet**:
   ```
   dotnet nuget push bin/Release/Blackbird.Filters.<version>.nupkg -k <your-api-key> -s https://api.nuget.org/v3/index.json
   ```

4. **Alternative: Use the NuGet CLI**:
   ```
   nuget push bin/Release/Blackbird.Filters.<version>.nupkg -ApiKey <your-api-key> -Source https://api.nuget.org/v3/index.json
   ```

5. **Alternative: Go to NuGet.org** and upload manually.

### Package Configuration

The package configuration is defined in the `.csproj` file:

- **PackageId**: The unique identifier for the package
- **Version**: The version of the package
- **Authors**: The authors of the package
- **Description**: A description of the package
- **PackageTags**: Tags to help users find the package
- **PackageLicenseExpression**: The license for the package
- **PackageProjectUrl**: The URL for the project
- **PackageIcon**: The icon for the package

Make sure these properties are properly set in the `.csproj` file before publishing.