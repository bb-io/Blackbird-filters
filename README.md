# Blackbird.Filters

Blackbird.Filters is a .NET library for processing and transforming content between different formats, with a focus on HTML content and XLIFF (XML Localization Interchange File Format) files. It provides tools for content extraction, transformation, and serialization.

## Main Library Structure

![1750852119651](image/readme/1750852119651.png)

The library is organized around two main components:

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

## Main Classes

### CodedContent

`CodedContent` is the core class for representing content extracted from a file:

- **Original**: The original file content as plain text
- **TextUnits**: A list of extracted text units
- **CreateTransformation**: Creates a transformation object from the coded content

### Transformation

`Transformation` represents a transformation of content from one language to another:

- **GetUnits**: Gets all units recursively
- **GetSegments**: Gets all segments recursively
- **Source**: Gets the source as coded content
- **Target**: Gets the target as coded content
- **TryParse**: Static method to parse content from a string or stream. The string or stream can contain XLIFF or HTML content, both will be deserialized using the appropriate serializer.

## Code example
```cs
    public string TranslateFile(string fileContent)
    {
        // File content can be either HTML or XLIFF (more formats to follow soon)
        var transformation = Transformation.TryParse(fileContent);

        foreach(var segment in transformation.GetSegments()) // You can also add .batch() to batch segments
        {
            // Implement API calls here
            segment.SetTarget(segment.GetSource() + " - Translated!"); 

            // More state manipulations can be performed here
            segment.State = SegmentState.Translated; 
        }

        // To continue and pass it as a transformation
        return Xliff2Serializer.Serialize(transformation);

        // To get the target as HTML:
        return HtmlContentCoder.Serialize(transformation.Target());
    }
```

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